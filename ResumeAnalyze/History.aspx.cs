using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace ResumeAnalyze
{
    public partial class History : System.Web.UI.Page
    {
        private const string ConnStrName = "DefaultConnection";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindGrid();
            }
        }

        protected void BtnRefresh_Click(object sender, EventArgs e)
        {
            TxtSearch.Text = "";
            BindGrid();
        }

        protected void BtnSearch_Click(object sender, EventArgs e)
        {
            BindGrid(TxtSearch.Text);
        }

        private void BindGrid(string keyword = null)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnStrName].ConnectionString))
            using (var da = new SqlDataAdapter(@"
            SELECT TOP 200 *
            FROM dbo.v_AnalysisHistory
            WHERE (@kw IS NULL OR @kw='' OR JobTitle LIKE '%'+@kw+'%' OR Company LIKE '%'+@kw+'%')
            ORDER BY CreatedAt DESC", conn))
            {
                da.SelectCommand.Parameters.AddWithValue("@kw", (object)keyword ?? DBNull.Value);
                var dt = new DataTable(); da.Fill(dt);
                GridHistory.DataSource = dt;
                GridHistory.DataBind();
                LblInfo.Text = "共 " + dt.Rows.Count + " 条记录";
            }
        }

        // ★ 与 .aspx 的 OnRowDataBound 一致；用于设置判定徽章样式
        protected void GridHistory_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            // 取 Score
            int score = 0;
            var drv = e.Row.DataItem as System.Data.DataRowView;
            if (drv != null && int.TryParse(drv["Score"].ToString(), out int s)) score = s;

            var badge = e.Row.FindControl("VerdictBadge") as HtmlGenericControl;
            if (badge != null)
            {
                badge.InnerText = VerdictText(score);
                badge.Attributes["class"] = "badge " + VerdictClass(score);
            }
        }

        private string VerdictText(int score)
        {
            if (score >= 75) return "Ready";
            if (score >= 55) return "Improve";
            return "Major Gap";
        }

        private string VerdictClass(int score)
        {
            if (score >= 75) return "ok";
            if (score >= 55) return "warn";
            return "bad";
        }
    }
}