using Diag.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Diag.Controllers
{
    public class codepostalController : ApiController
    {
        public HttpResponseMessage Get()
        {
            string query = @"
select *
from dbo.codepostal";
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

        public String post(codepostal user)
        {
            try
            {

                string query = @"INSERT INTO dbo.codepostal (codepostal) VALUES ('" + user.codepostall + "')";

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



        public String put(codepostal q)
        {
            try
            {

                string query = @"UPDATE dbo.codepostal SET  codepostal= '" + q.codepostall + "' WHERE id = " + q.id + ";";

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




        public String Delete(int id)
        {
            try
            {
                string query = @"DELETE from dbo.codepostal WHERE id =" + id + @" ";

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
