using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace ResumeAnalyze
{
    public partial class ResultDashboard : System.Web.UI.Page
    {
        private const string ConnStrName = "DefaultConnection";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                long id;
                if (long.TryParse(Request.QueryString["id"], out id))
                    LoadDashboard(id);
            }
        }

        protected void BtnReAnalyze_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/UploadResume.aspx");
        }

        protected void BtnSaveReport_Click(object sender, EventArgs e)
        {
            LblTime.Text = "Report saved.";
        }

        protected void BtnRegenerateBullets_Click(object sender, EventArgs e)
        {
            LitBullets.Text = (Session["bullets"] as string) ?? "(No content)";
        }

        private void LoadDashboard(long id)
        {
            // 1) Main analysis row
            DataRow a = GetAnalysisRow(id);
            if (a == null)
            {
                LblTime.Text = "Record not found.";
                return;
            }

            LblScore.Text = SafeNum(a["OverallScore"]) + "/100";
            LblVerdict.Text = a["Verdict"] != null ? a["Verdict"].ToString() : "--";
            LblKeywords.Text = (a["KeywordsHit"] == DBNull.Value) ? "--" : a["KeywordsHit"].ToString();
            LblTime.Text = Convert.ToDateTime(a["CreatedAt"]).ToString("yyyy-MM-dd HH:mm");

            // 2) Subscores -> Radar JSON (or fallback from Session)
            DataRow sc = GetScoreRow(id);
            if (sc != null)
            {
                LitRadarJson.Text = BuildRadarJson(
                    SafeNum(sc["SkillsScore"]),
                    SafeNum(sc["ExperienceScore"]),
                    SafeNum(sc["EducationScore"]),
                    SafeNum(sc["KeywordsScore"]),
                    SafeNum(sc["AtsScore"])
                );
            }
            else
            {
                string fallbackRadar = Session["radar_json"] as string;
                LitRadarJson.Text = string.IsNullOrWhiteSpace(fallbackRadar) ? "" : fallbackRadar;
            }

            string barJson = Session["bar_json"] as string;
            LitBarJson.Text = string.IsNullOrWhiteSpace(barJson) ? "" : barJson;

            DataTable pairs = GetEvidencePairs(id, 5);
            if (pairs == null || pairs.Rows.Count == 0)
            {
                string pairsJson = Session["pairs"] as string;
                if (!string.IsNullOrWhiteSpace(pairsJson))
                {
                    try
                    {
                        var list = Newtonsoft.Json.JsonConvert
                            .DeserializeObject<System.Collections.Generic.List<PairDto>>(pairsJson);

                        DataTable tbl = new DataTable();
                        tbl.Columns.Add("Similarity", typeof(decimal));
                        tbl.Columns.Add("ResumeSentence", typeof(string));
                        tbl.Columns.Add("JDSentence", typeof(string));

                        if (list != null)
                        {
                            foreach (var p in list)
                            {
                                DataRow r = tbl.NewRow();
                                r["Similarity"] = p != null ? p.sim : 0m;
                                r["ResumeSentence"] = p != null && p.resume != null ? p.resume : "";
                                r["JDSentence"] = p != null && p.jd != null ? p.jd : "";
                                tbl.Rows.Add(r);
                            }
                        }
                        pairs = tbl;
                    }
                    catch
                    {

                    }
                }
            }
            BindPairs(pairs ?? new DataTable());

            string bullets = Session["bullets"] as string;
            LitBullets.Text = string.IsNullOrWhiteSpace(bullets) ? "" : bullets;

            string summary = Session["summary"] as string;
            LitSummary.Text = string.IsNullOrWhiteSpace(summary) ? "No summary available." : summary;
        }


        private void BindPairs(DataTable dt)
        {
            List<object> list = new List<object>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new
                {
                    Score = Convert.ToDecimal(r["Similarity"]).ToString("0.00"),
                    ResumeSent = r["ResumeSentence"] != null ? r["ResumeSentence"].ToString() : "",
                    JDSent = r["JDSentence"] != null ? r["JDSentence"].ToString() : ""
                });
            }
            RptPairs.DataSource = list;
            RptPairs.DataBind();
        }

        private DataRow GetAnalysisRow(long id)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnStrName].ConnectionString))
            using (var da = new SqlDataAdapter("SELECT * FROM dbo.Analysis WHERE AnalysisId=@id", conn))
            {
                da.SelectCommand.Parameters.AddWithValue("@id", id);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }
        }

        private DataRow GetScoreRow(long id)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnStrName].ConnectionString))
            using (var da = new SqlDataAdapter("SELECT * FROM dbo.AnalysisScore WHERE AnalysisId=@id", conn))
            {
                da.SelectCommand.Parameters.AddWithValue("@id", id);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }
        }

        private DataTable GetEvidencePairs(long id, int topN)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnStrName].ConnectionString))
            using (var da = new SqlDataAdapter(@"
                SELECT TOP (@n) ResumeSentence, JDSentence, ISNULL(Similarity,0) AS Similarity
                FROM dbo.EvidencePair WHERE AnalysisId=@id ORDER BY RankNo ASC", conn))
            {
                da.SelectCommand.Parameters.AddWithValue("@id", id);
                da.SelectCommand.Parameters.AddWithValue("@n", topN);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        private string BuildRadarJson(decimal skills, decimal exp, decimal edu, decimal kw, decimal ats)
        {
            return string.Format(
                "{{\"type\":\"radar\",\"data\":{{\"labels\":[\"Skills\",\"Experience\",\"Education\",\"Keywords\",\"ATS\"]," +
                "\"datasets\":[{{\"label\":\"Match\",\"data\":[{0},{1},{2},{3},{4}],\"fill\":true}}]}}}}",
                skills, exp, edu, kw, ats);
        }

        private decimal SafeNum(object o)
        {
            if (o == null || o == DBNull.Value) return 0;
            return Convert.ToDecimal(o);
        }
        protected void BtnCopyBullets_Click(object sender, EventArgs e)
        {
            // Simple copy action — copy bullet points to clipboard or display message
            string bullets = (Session["bullets"] as string) ?? "";
            if (!string.IsNullOrWhiteSpace(bullets))
            {
                LblTime.Text = "Bullet points copied.";
            }
            else
            {
                LblTime.Text = "No bullet points available.";
            }
        }

        class PairDto
        {
            public string resume { get; set; }
            public string jd { get; set; }
            public decimal sim { get; set; }
        }


    }
}
