using Diag.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Azure.Storage.Blobs;


namespace Diag.Controllers
{
    public class AccountController : ApiController
    {
        public HttpResponseMessage Get()
        {
            string query = @"
select *
from dbo.Account
where role='client'";
            DataTable table = new DataTable();
            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
            using (var cmd = new SqlCommand(query, con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                da.Fill(table);
            }

            return Request.CreateResponse(HttpStatusCode.OK, table);
        }

        public String post(Account user)
        {
            try
            {

                string query = @"INSERT INTO dbo.Account (username, email,password, phone , role) VALUES ('" + user.username + "','" + user.email + "','" + user.coverttoencrypt(user.password) + "','" + user.phone  + "','" + user.role + "')";

                DataTable table = new DataTable();
                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
                using (var cmd = new SqlCommand(query, con))
                using (var da = new SqlDataAdapter(cmd))
                {
                    cmd.CommandType = CommandType.Text;
                    da.Fill(table);
                }

                return "added successful !!";
            }
            catch (Exception)
            {
                return "failed to add !!";
            }
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/SignUp/VerifAccount")]

        public Boolean VerifAccount(Account log)
        {

            string query = @"select email from dbo.Account where email='" + log.email + "'";
            DataTable table = new DataTable();
            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
            using (var cmd = new SqlCommand(query, con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                da.Fill(table);

            }

            if (table.Rows.Count > 0)
            {
                return true;

            }
            else
            {
                return false;
            }

        }

        public String put(Account q)
        {
            try
            {

                string query = @"UPDATE dbo.Account SET  username= '" + q.username + "',email = '" + q.email +  "', password = '" + q.coverttoencrypt(q.password) + "', phone ='" + q.phone + "' WHERE id = " + q.id + ";";

                DataTable table = new DataTable();
                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
                using (var cmd = new SqlCommand(query, con))
                using (var da = new SqlDataAdapter(cmd))
                {
                    cmd.CommandType = CommandType.Text;
                    da.Fill(table);
                }

                return " yessss  update successful !!";
            }
            catch (Exception)
            {
                return "failed to update !!";
            }
        }

        [System.Web.Http.HttpPut]
        [System.Web.Http.Route("api/Account/updateVideo")]
        public string UpdateVideo(video i)
        {
            try
            {
                string query = @"UPDATE dbo.Videos 
                         SET pageName = @PageName, 
                             VideoSrc = @VideoSrc 
                         WHERE pageName = @OldPageName;";

                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
                using (var cmd = new SqlCommand(query, con))
                {

                    cmd.Parameters.AddWithValue("@PageName", i.pageName);
                    cmd.Parameters.AddWithValue("@VideoSrc", i.VideoSrc);
                    cmd.Parameters.AddWithValue("@OldPageName", i.pageName);

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return "Update successful!";
                    }
                    else
                    {
                        return "No records were updated.";
                    }
                }
            }
            catch (Exception ex)
            {
                return "Failed to update: " + ex.Message;
            }
        }

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/Account/getVideos")]
        public HttpResponseMessage getVideos()
        {
            string query = @"
select *
from dbo.Videos
";
            DataTable table = new DataTable();
            using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
            using (var cmd = new SqlCommand(query, con))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.Text;
                da.Fill(table);
            }

            return Request.CreateResponse(HttpStatusCode.OK, table);
        }



        private readonly string[] allowedExtensions = { ".mp4", ".avi", ".mkv" };
        private const int MaxFileSize = 100 * 1024 * 1024; // 100 Mo
        private const string StorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=tagadiagstorage;AccountKey=jyp0UeGuE1sWTRlQT+Oy3j+3k+PUPek2pthCEhTknEb0kBVxSYP/OUdtjHQ9Sn9MZPFifuo+dgfR+AStf8RMAw==;EndpointSuffix=core.windows.net";
        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("api/Account/SaveFileVideo")]
        public async Task<IHttpActionResult> SaveFileVideo()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;

                if (httpRequest.Files.Count == 0)
                {
                    return BadRequest("No file uploaded");
                }

                var postedFile = httpRequest.Files["uploadedFile"];
                if (postedFile == null)
                {
                    return BadRequest("Invalid file input");
                }

                string containerName = "tagadiagblob";
                string originalFileName = Path.GetFileName(postedFile.FileName); // Keep original name

                // Optional: Sanitize filename (removes special characters)
                originalFileName = originalFileName.Replace(" ", "_");

                var blobServiceClient = new BlobServiceClient(StorageConnectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blockBlobClient = blobContainerClient.GetBlobClient(originalFileName);

                using (var stream = postedFile.InputStream)
                {
                    await blockBlobClient.UploadAsync(stream, true);
                }

                return Ok(new
                {
                    filename = originalFileName, // Returns original filename
                    url = blockBlobClient.Uri.ToString()
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error: " + ex.Message));
            }
        }



        //public IHttpActionResult SaveFileVideo()
        //{
        //    try
        //    {
        //        var httpRequest = HttpContext.Current.Request;

        //        if (httpRequest.Files.Count == 0)
        //        {
        //            return BadRequest("Aucun fichier téléchargé.");
        //        }

        //        var postedFile = httpRequest.Files[0];
        //        var originalFileName = Path.GetFileName(postedFile.FileName);

        //        var fileExtension = Path.GetExtension(postedFile.FileName).ToLower();
        //        var fileSize = postedFile.ContentLength;

        //        // Vérification de l'extension
        //        if (!allowedExtensions.Contains(fileExtension))
        //        {
        //            return BadRequest("Format non supporté. Formats autorisés : MP4, AVI, MKV.");
        //        }

        //        // Vérification de la taille du fichier
        //        if (fileSize > MaxFileSize)
        //        {
        //            return BadRequest("Fichier trop volumineux. Taille maximale autorisée : 100 Mo.");
        //        }

        //        // Génération d'un nom unique pour éviter l'écrasement et les attaques
        //        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        //        var savePath = HttpContext.Current.Server.MapPath("~/videos/" + originalFileName);

        //        // Sauvegarde du fichier
        //        postedFile.SaveAs(savePath);

        //        return Ok(new { FileName = originalFileName, Message = "Vidéo téléchargée avec succès." });
        //    }
        //    catch (Exception ex)
        //    {
        //        return InternalServerError(ex);
        //    }
        //}


        public String Delete(int id)
        {
            try
            {
                string query = @"DELETE from dbo.Account WHERE id =" + id + @" ";

                DataTable table = new DataTable();
                using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings["diag"].ConnectionString))
                using (var cmd = new SqlCommand(query, con))
                using (var da = new SqlDataAdapter(cmd))
                {
                    cmd.CommandType = CommandType.Text;
                    da.Fill(table);
                }

                return "delete successful !!";
            }
            catch (Exception)
            {
                return "failed to delete !!";
            }
        }

    }

}