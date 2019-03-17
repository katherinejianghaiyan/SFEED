using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Configuration;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using Utils;
using Utils.Common;
using Utils.Database.SqlServer;
using Model.Common;
using Utils.Net;

namespace WeChatEvent
{
    public static class PersonPicHelper
    {
        private static string conn = ConfigurationManager.ConnectionStrings["Gladis"].GetString();
        private static string lucyDrawUrl = ConfigurationManager.AppSettings["LucyDrawUrl"].GetString();
        public static PersonPic CheckEncyptedEmployeeID(string key)
        {
            try
            {
                string tmpId = EncyptHelper.DesEncypt(key);
                string sql = "Select EmployeeID,EmployeeName,ImageType,ImageData from [dbo].[TmpPersonImage] Where WeChatID='" + tmpId + "'";
                DataTable data = new DBHelper(conn).GetDataTable(sql);
                if (data == null || data.Rows.Count != 1) return null;
                PersonPic retData = new PersonPic();
                retData.EmpID = data.Rows[0].Field<string>("EmployeeID").GetString();
                retData.EmpName = data.Rows[0].Field<string>("EmployeeName").GetString();
                string type = data.Rows[0].Field<string>("ImageType").GetString();
                if (string.IsNullOrWhiteSpace(type)) retData.PicData = string.Empty;
                else retData.PicData = type + "," + Convert.ToBase64String(data.Rows[0].Field<byte[]>("ImageData"));
                return retData;
            }
            catch { return null; }
        }

        public static bool SavePicData(PersonPic data)
        {
            try
            {
                if (data.Token.GetString().Equals("adenservices"))
                {
                    if (string.IsNullOrWhiteSpace(data.PicData) || string.IsNullOrWhiteSpace(data.EmpID)) return false;
                    string[] imgData = data.PicData.Split(',');
                    Bitmap destData = new Bitmap(360, 270);
                    using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(imgData[1])))
                    {
                        Bitmap orignData = new Bitmap(ms);
                        Graphics g = Graphics.FromImage(destData);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(orignData, new Rectangle(0, 0, 360, 270), new Rectangle(0, 0, orignData.Width, orignData.Height), GraphicsUnit.Pixel);
                    }
                    System.Data.SqlClient.SqlParameter idata = new System.Data.SqlClient.SqlParameter("@data", SqlDbType.Image);
                    using (MemoryStream ms1 = new MemoryStream())
                    {
                        destData.Save(ms1, ImageFormat.Png);
                        byte[] arr = new byte[ms1.Length];
                        ms1.Position = 0;
                        ms1.Read(arr, 0, (int)ms1.Length);
                        idata.Value = arr;
                    }
                    string sql = "update [dbo].[TmpPersonImage] set ImageType=@type,ImageData=@data where EmployeeID=@empid";
                    System.Data.SqlClient.SqlParameter type = new System.Data.SqlClient.SqlParameter("@type", SqlDbType.VarChar, 50);
                    type.Value = imgData[0];
                   
                    System.Data.SqlClient.SqlParameter eid = new System.Data.SqlClient.SqlParameter("@empid", SqlDbType.VarChar, 10);
                    eid.Value = data.EmpID;
                    int count = new DBHelper(conn).Execute(sql, CommandType.Text, new System.Data.SqlClient.SqlParameter[] { type, idata, eid });
                    if (count > 0) return true;
                    else return false;
                }
                else return false;
            }
            catch(Exception e) { return false; }
        }

        public static string Signed(string weChatID, string eventKey)
        {
            string returnVal = "error";
            if (string.IsNullOrWhiteSpace(eventKey)) return returnVal;         
            string key = "adenannualparty";
            string updateSql = "update [dbo].[TmpPersonImage] set status=1 where isnull(msg,'')='' and status=0 and WeChatID='" + weChatID + "'";
            int count = 0;
            if (key.Equals(eventKey))
            {
                returnVal = "matched";
                count = new DBHelper(conn).Execute(updateSql);
            }
            else if (eventKey.StartsWith("qrscene_") && eventKey.Split('_')[1].GetString().Equals(key))
            {
                returnVal = "matched";
                count = new DBHelper(conn).Execute(updateSql);
            }
            if (count > 0)
            {
                DataTable dt = new DBHelper(conn).GetDataTable("select ID,EmployeeID,EmployeeName from [dbo].[TmpPersonImage] where isnull(msg,'')='' and status=1 and WeChatID='" + weChatID + "'");
                if (dt != null && dt.Rows.Count == 1)
                {
                    try
                    {
                        new HttpHelper().Get(lucyDrawUrl + "/kongzhi/?token=adenservices&status=join&src=" + dt.Rows[0].Field<int>("ID").GetString()
                            + "&ename=" + dt.Rows[0].Field<string>("EmployeeName").GetString() + "&eid=" + dt.Rows[0].Field<string>("EmployeeID").GetString());
                    }
                    catch { }
                }
                returnVal = "ok";
            }
            return returnVal;
        }

        public static BaseResponse BindingUser(Person data)
        {
            BaseResponse response = new BaseResponse();
            try
            { 
                if (data.Token.GetString().Equals("adenservices") && !string.IsNullOrWhiteSpace(data.WeChatID.GetString()))
                {
                    DBHelper helper = new DBHelper(conn);
                    //1判断用户是否已经绑定
                    string sql = "select ID from [dbo].[TmpPersonImage] where isnull(WeChatID,'')='" + data.WeChatID.GetString() + "'";
                    if (helper.GetDataScalar(sql).ToInt() > 0)
                    {
                        response.Status = "ok";
                        response.Msg = Utils.Common.EncyptHelper.Encypt(data.WeChatID.GetString());
                    }
                    else
                    {
                        //2用户选择
                        sql = "select ID,WeChatID from [dbo].[TmpPersonImage] where ";
                        if(!string.IsNullOrWhiteSpace(data.EmployeeID.GetString())) sql += "EmployeeID='" + data.EmployeeID.GetString() + "'";
                        else sql += "EmployeeName='" + data.Name.GetString() + "'";
                        DataTable dt = helper.GetDataTable(sql);
                        if (dt == null || dt.Rows.Count == 0)
                        {
                            response.Status = "error";
                            response.Msg = "用户未找到,请联系公司!";
                        }
                        else
                        {
                            if (dt.Rows.Count > 1)
                            {
                                response.Status = "error";
                                response.Msg = "存在多个同名用户,请输入员工号,精确匹配!";
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(dt.Rows[0].Field<string>("WeChatID").GetString()))
                                {
                                    response.Status = "error";
                                    response.Msg = "该用户已被其他微信帐号绑定,请联系公司!";
                                }
                                else
                                {
                                    int id = dt.Rows[0].Field<int>("ID");
                                    int count = helper.Execute("update [dbo].[TmpPersonImage] set WeChatID='" + data.WeChatID.GetString()
                                              + "' where ID=" + id);
                                    if (count > 0)
                                    {
                                        response.Status = "ok";
                                        response.Msg = Utils.Common.EncyptHelper.Encypt(data.WeChatID.GetString());
                                    }
                                    else
                                    {
                                        response.Status = "error";
                                        response.Msg = "更新数据库出错,请联系公司!";
                                    }

                                }
                            }
                        }
                    }  
                    
                }
                else
                {
                    response.Status = "error";
                    response.Msg = "Token错误或者微信ID为空";
                }
            }
            catch { response.Status = "error"; response.Msg = "程序出错,请联系公司!"; }

            return response;
        }
    }
}
