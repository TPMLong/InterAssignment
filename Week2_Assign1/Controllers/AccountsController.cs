using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Week2_Assign1.Data;
using Week2_Assign1.Models;

namespace Week2_Assign1.Controllers
{
    //Auto change controller the fit the route
    //in this case will be "../api/Accounts" because we have AccountsController
    //not "../api/AccountsController" because AccountsController will become 'Accounts / Controller' => so when u type Accounts it auto go to AccountsController
    //if you type AccountsController it will go to AccountsControllerController => wrong path
    /// <summary>>

    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        //some field need to remember...
        //like the name readonly mean this field will read only, you can't change it, avoid someone want to do something bad, hehehe....
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly IConfigurationSection _jwtSettings;
        private readonly DBContext _dBContext;

        public AccountsController(IMapper mapper, UserManager<User> userManager, IConfiguration configuration, DBContext dBContext)
        {
            _mapper = mapper;
            _userManager = userManager;
            _jwtSettings = configuration.GetSection("JwtSettings");
            _dBContext = dBContext;
        }

        //contructor
        //to make this quick, use can select 3 field on the top, press ">" and Enter to create the contructor below
        /*        
        public AccountsController(IMapper mapper, UserManager<User> userManager, IConfigurationSection jwtSettings){
            _mapper = mapper;
            _userManager = userManager;
            _jwtSettings = jwtSettings;
        }
        */
        // =))) if you want type by hand, type "ctor" and press tab is enough...
        //the contructor below i have 
        /*        public AccountsController(IMapper mapper, UserManager<User> userManager, IConfiguration configuration)
                {
                    _mapper = mapper;
                    _userManager = userManager;
                    //again, this get value configurate from "JWTSettings"
                    _jwtSettings = configuration.GetSection("JwtSettings");
                }*/


        //type Register at the end of the URL to use this fuction.
        //like this "../api/Accounts/Register"
        //because this is a post HTTP method so to test API we will go to body at raw, using JSON, and type the data we want to test
        //[Authorize] authorize because we don't need to no
        [HttpPost("Register")]
        public async Task<ActionResult> Register(UserRegistrationModel userModel)
        {
            //map the type user at userModel
            var user = _mapper.Map<User>(userModel);
            //method provide to create user
            var result = await _userManager.CreateAsync(user, userModel.Password);
            if (!result.Succeeded)
            {
                return Ok(result.Errors);
            }
            //method to set role for a user
            await _userManager.AddToRoleAsync(user, "Visitor");
            //201 HTTP method mean created
            return StatusCode(201);
        }

        //read more about reflection

        //type Login at the end of the URL to use this fuction.
        //like this "../api/Accounts/Login"
        //because this is a post HTTP method so to test API we will go to body at raw, using JSON, and type the data we want to test
        //[Authorize] 
        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserLoginModel userModel)
        {
            //method provide to search correct email
            var user = await _userManager.FindByEmailAsync(userModel.Email);
            //check to see if the user is avaiable or not and the field password is correct to that user
            if (user != null && await _userManager.CheckPasswordAsync(user, userModel.Password) && user.isDelete != true)
            {
                //return a hash key 
                var signingCredentials = GetSigningCredentials();

                //this claim hold user information
                var claims = GetClaims(userModel);
                //Initializes a new instance of the GenerateTokenOptions to create a JWT
                var tokenOptions = GenerateTokenOptions(signingCredentials, await claims);
                //create a token
                var token = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
                //a token will be return, you can use this token to get user information
                return Ok(token);
            }
            return Unauthorized("Invalid Authentication");
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            User exUser = await _userManager.FindByIdAsync(id);
            //var user = _mapper.Map<User>(userModel);
            if (exUser != null)
            {
                exUser.isDelete = true;
                var result = await _userManager.UpdateAsync(exUser);
                if (!result.Succeeded)
                {
                    return Ok(result.Errors);
                }
                return Ok("delete success");
            }
            else
            {
                return NotFound($"Can't find user with id {id}");
            }
        }

        [HttpGet]
        public IActionResult GetUser()
        {
            //map the type user at userModel
            var User =  _dBContext.Users.ToList();
            //var user = _mapper.Map<User>(userModel);
            if (User != null)
            {
                return Ok(User);
            }
           else
           {
              return NotFound("Empty");
           }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetUser(string id)
        {
            //map the type user at userModel
            var exUser = await _userManager.FindByIdAsync(id);
            //var user = _mapper.Map<User>(userModel);
            if(exUser != null)
            {
                return Ok(exUser);
            }
            else
            {
                return NotFound($"Can't find user with id {id}");
            }
        }

        [HttpPatch("Update/{id}")]
        public async Task<ActionResult> Update(string id, UserGetModel userModel)
        {
            //map the type user at userModel
            var user = _mapper.Map<User>(userModel);
            user.Id = id;
            //method provide to update user
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Ok(result.Errors);
            }
            return Ok($"Update success");
        }


        [HttpPost("Reset/{email}")]
        public async Task<ActionResult> Reset(string email)
        {
            User exUser = await _userManager.FindByEmailAsync(email);
            // var resetPasswordToken = await _userManager.GeneratePasswordResetTokenAsync(exUser);
            if (exUser != null)
            {
                var resetPasswordToken = await _userManager.GeneratePasswordResetTokenAsync(exUser);
                var apiKey = "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";
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("trieuminhlong2000@gmail.com", "Long");
                // var to = new EmailAddress(email, "Hello");
                var to = new EmailAddress("longtpmse140775@gmail.com", "Hello");
                var subject = "new password";
                var plainTextContent = resetPasswordToken;
                var htmlContent = "<strong>what is this?</strong>";
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                var response = await client.SendEmailAsync(msg);
                return Ok(resetPasswordToken);
            }
            else
            {
                return NotFound("Wrong email");
            }
        }











        private SigningCredentials GetSigningCredentials()
        {
            //encodes securityKey value into a sequence of bytes
            var key = Encoding.UTF8.GetBytes(_jwtSettings.GetSection("securityKey").Value);
            //the var will have the size, in bits, of the key.
            var secret = new SymmetricSecurityKey(key);
            //hash
            return new SigningCredentials(secret, SecurityAlgorithms.HmacSha256);
        }

        //JwtSecurityToken(JwtHeader, JwtPayload)
        private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            var tokenOptions = new JwtSecurityToken(
            issuer: _jwtSettings.GetSection("validIssuer").Value,
            audience: _jwtSettings.GetSection("validAudience").Value,
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(_jwtSettings.GetSection("expiryInMinutes").Value)),
            signingCredentials: signingCredentials);
            return tokenOptions;
        }


        private async Task<List<Claim>> GetClaims(UserLoginModel userModel)
        {
            var user = await _userManager.FindByEmailAsync(userModel.Email);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email)
            };
            //get user role
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            return claims;
        }
    }
}
