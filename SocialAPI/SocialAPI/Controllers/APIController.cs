using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.IdentityModel.Tokens;
using SocialAPI.Data;
using SocialAPI.Model;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace SocialAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class APIController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public APIController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private int GetUserID()
        {
            // get user ID from JWT
            return int.Parse(this.User.Claims.First(i => i.Type == "UserId").Value);
        }

        private string GetFullName()
        {
            return this.User.Claims.First(i => i.Type == "FullName").Value;
        }

        [HttpPost]
        public bool RegisterUser(string UserName, string Password, string FullName)
        {
            var connString = _configuration.GetConnectionString("TestDB");
            try
            {
                if (string.IsNullOrEmpty(UserName) ||  string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(FullName))
                {
                    return false;
                }

                using (var connection = new SqlConnection(connString))
                {
                    // check if user name is occupied
                    var sql = "SELECT COUNT(*) FROM TBL_USER_ACCOUNT WHERE USERNAME=@USERNAME";
                    var newUser = new { USERNAME = UserName };
                    var checkUser = connection.ExecuteScalar<int>(sql, newUser);
                    if (checkUser > 0) // got records, means that user name has been used
                        return false;

                    // create new user account
                    sql = "INSERT INTO TBL_USER_ACCOUNT (USERNAME, PASSWORD, FULLNAME) VALUES (@USERNAME, @PASSWORD, @FULLNAME)";
                    var newAccount = new { USERNAME = UserName, PASSWORD = Password, FULLNAME = FullName };
                    var rowsAffected = connection.Execute(sql, newAccount);
                    if (rowsAffected >= 1)
                        return true;
                }
            }
            catch 
            {
                return false;
            }

            return false;
        }

        [HttpGet]
        public string Login(string UserName, string Password)
        {
            try
            {
                UserAccount account = new();

                // first validate if the user exists and password is correct or not
                var connString = _configuration.GetConnectionString("TestDB");
                using (var connection = new SqlConnection(connString))
                {
                    var sql = "SELECT * FROM TBL_USER_ACCOUNT WHERE USERNAME=@USERNAME AND PASSWORD=@PASSWORD";
                    var param = new { USERNAME = UserName, PASSWORD = Password };
                    var result = connection.QueryFirstOrDefault<UserAccount>(sql, param);

                    if (result == null)
                        return "Login unsuccess. User Name or Password are wrong.";

                    account = result;
                }

                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
                var signingCredentials = new SigningCredentials(
                                        new SymmetricSecurityKey(key),
                                        SecurityAlgorithms.HmacSha512Signature
                                    );

                var subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, account.UserName),
                    new Claim("UserId", account.UserID.ToString()),
                    new Claim("FullName", account.FullName)
                });

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = subject,
                    Expires = DateTime.UtcNow.AddMinutes(10), // expire in 10 minutes
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = signingCredentials
                };
                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = tokenHandler.WriteToken(token);

                return jwtToken; // return JWT token
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        [HttpPost]
        [Authorize]
        public bool CreatePost(string Content, IFormFile? Image = null)
        {
            try
            {
                int userID = GetUserID(); // get User ID from JWT token
                string fullName = GetFullName();
                string sql = "";
                var connString = _configuration.GetConnectionString("TestDB");
                using (var connection = new SqlConnection(connString))
                {
                    // process the uploaded image if available
                    if (Image != null && Image.Length > 0)
                    {
                        string fileName = Image.FileName;
                        byte[] fileStream;

                        using (var memoryStream = new MemoryStream())
                        {
                            Image.CopyTo(memoryStream);
                            fileStream = memoryStream.ToArray();
                        }

                        sql = "INSERT INTO TBL_USER_POST (USERID,CREATEDTIME,CONTENT,[IMAGE],LIKES,IMAGENAME,FULLNAME) VALUES " +
                            "(@USERID, CURRENT_TIMESTAMP, @CONTENT, @IMAGE, 0, @IMAGENAME, @FULLNAME)";
                        var param = new 
                        { 
                            USERID = userID, CONTENT = Content, IMAGE = fileStream, IMAGENAME = fileName, FULLNAME = fullName 
                        };
                        var rowsAffected = connection.Execute(sql, param);
                        if (rowsAffected > 0)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // no image is uploaded
                        sql = "INSERT INTO TBL_USER_POST (USERID,CREATEDTIME,CONTENT,LIKES,FULLNAME) VALUES " +
                            "(@USERID, CURRENT_TIMESTAMP, @CONTENT, 0, @FULLNAME)";
                        var param = new { USERID = userID, CONTENT = Content, FULLNAME = fullName };
                        var rowsAffected = connection.Execute(sql, param);
                        if (rowsAffected > 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        [HttpGet]
        public List<UserPost> GetPosts(int? UserId = null, DateTime? DateFrom = null, DateTime? DateTo = null)
        {
            List<UserPost> posts = new List<UserPost>();

            // if got parameters, then get post by param, else return all posts
            try
            {
                var connString = _configuration.GetConnectionString("TestDB");

                // build SQL based on parameters pass in 
                string sql = "SELECT * FROM TBL_USER_POST";
                string condition = "";
                if(UserId.HasValue)
                {
                    condition = "USERID=@USERID";
                }
                if(DateFrom.HasValue)
                {
                    if (condition == "")
                        condition = "CREATEDTIME>=@DATEFROM";
                    else
                        condition += " AND CREATEDTIME>=@DATEFROM";
                }
                if (DateTo.HasValue)
                {
                    if (condition == "")
                        condition = "CREATEDTIME<=@DATETO";
                    else
                        condition += " AND CREATEDTIME<=@DATETO";
                }
                if (!string.IsNullOrEmpty(condition.Trim()))
                    sql += " WHERE " + condition;

                // prepare parameters
                var param = new { USERID = UserId, DATEFROM = DateFrom, DATETO = DateTo };

                using (var connection = new SqlConnection(connString))
                {
                    posts = connection.Query<UserPost>(sql, param).ToList();

                    // get comment for the posts
                    foreach (var post in posts)
                    {
                        sql = "SELECT * FROM TBL_USER_COMMENT WHERE POSTID=@POSTID";
                        var param2 = new { POSTID = post.PostID };
                        post.Comments = connection.Query<UserComment>(sql, param2).ToList();
                    }
                }

                // return posts with comments if any
                return posts;
            }
            catch
            {
                return posts;
            }
        }

        [HttpPut]
        [Authorize]
        public bool LikeOrUnlikePost(int PostId)
        {
            try
            {
                int userID = GetUserID();
                var connString = _configuration.GetConnectionString("TestDB");
                using (var connection = new SqlConnection(connString))
                {
                    var sql = "spAPI_UpdateLikes";
                    var param  = new DynamicParameters();
                    param.Add("@POSTID", PostId);
                    param.Add("@USERID", userID);

                    var result = connection.QuerySingle<string>(sql, param, commandType: System.Data.CommandType.StoredProcedure);
                    if(result == "DONE UPDATE LIKE")
                        return true;
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        [HttpPost]
        [Authorize]
        public bool CommentPost(int PostId, string Comment)
        {
            try
            {
                var userID = GetUserID();
                var fullName = GetFullName();
                var connString = _configuration.GetConnectionString("TestDB");
                using (var connection = new SqlConnection(connString))
                {
                    var sql = "INSERT INTO TBL_USER_COMMENT (POSTID,USERID,COMMENT,CREATEDTIME,FULLNAME) VALUES" +
                        "(@POSTID, @USERID, @COMMENT, CURRENT_TIMESTAMP, @FULLNAME)";
                    var param = new { POSTID = PostId, USERID = userID, COMMENT = Comment, FULLNAME = fullName };
                    var result = connection.Execute(sql, param);
                    if(result > 0) 
                        return true;
                    else 
                        return false;
                }
            }
            catch
            { 
                return false; 
            }
        }

        [HttpGet]
        [Authorize]
        public List<UserPost> GetLikePost()
        {
            List<UserPost> posts = new List<UserPost>();

            try
            {
                var userID = GetUserID();
                var connString = _configuration.GetConnectionString("TestDB");
                using (var connection = new SqlConnection(connString))
                {
                    // get the records of the user liked post
                    var sql = "SELECT * FROM TBL_USER_POST WHERE POSTID IN (" +
                        "SELECT POSTID FROM TBL_USER_LIKES WHERE USERID=@USERID)";
                    var param = new { USERID = userID };
                    posts = connection.Query<UserPost>(sql, param).ToList();

                    // get the comment under the posts
                    foreach (var post in posts)
                    {
                        sql = "SELECT * FROM TBL_USER_COMMENT WHERE POSTID=@POSTID";
                        var param2 = new { POSTID = post.PostID };
                        post.Comments = connection.Query<UserComment>(sql, param2).ToList();
                    }
                }
                return posts;
            }
            catch
            {
                return posts;
            }
        }
    }
}
