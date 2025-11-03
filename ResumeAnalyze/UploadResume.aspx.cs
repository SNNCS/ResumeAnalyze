using System;
using System.IO;
using System.Net.Http;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Newtonsoft.Json;
using System.Web.UI.WebControls;

namespace ResumeAnalyze
{
    public partial class UploadResume : System.Web.UI.Page
    {
        private const string ConnStrName = "DefaultConnection";
        private const string FastApiBase = "http://localhost:8000";

        protected void BtnClear_Click(object sender, EventArgs e)
        {
            ResumeTextBox.Text = "";
            JdTextBox.Text = "";
            JdUrlTextBox.Text = "";
            LblStatus.Text = "Cleared.";
        }

        protected async void BtnAnalyze_Click(object sender, EventArgs e)
        {
            try
            {
                string resumeText = (ResumeTextBox.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(resumeText) && ResumeFileUpload.HasFile)
                {
                    using (var sr = new StreamReader(ResumeFileUpload.FileContent, Encoding.UTF8, true))
                        resumeText = sr.ReadToEnd();
                }

                string jdText = (JdTextBox.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(resumeText) || string.IsNullOrWhiteSpace(jdText))
                {
                    LblStatus.Text = "Please provide both resume and job description.";
                    return;
                }

                var payload = new
                {
                    resume = resumeText,
                    jd = jdText,
                    use_openai = ChkUseOpenAI.Checked,
                    enable_rerank = ChkEnableRerank.Checked
                };

                AnalysisResponse ai;
                using (var http = new HttpClient())
                {
                    http.BaseAddress = new Uri(FastApiBase);
                    http.Timeout = TimeSpan.FromSeconds(60);
                    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                    var resp = await http.PostAsync("/api/analyze", content);
                    resp.EnsureSuccessStatusCode();
                    var json = await resp.Content.ReadAsStringAsync();
                    ai = JsonConvert.DeserializeObject<AnalysisResponse>(json);
                }

                long analysisId = SaveAnalysis(
                    resumeText,
                    jdText,
                    ai != null && !string.IsNullOrWhiteSpace(ai.job_title) ? ai.job_title : GuessJobTitle(jdText),
                    ai != null ? ai.score : 0,
                    ai != null && !string.IsNullOrWhiteSpace(ai.verdict) ? ai.verdict : VerdictFromScore(ai != null ? ai.score : 0),
                    ai != null && ai.keywords_hit.HasValue ? ai.keywords_hit.Value : 0,
                    ai != null ? ai.scores : null
                );

                Session["summary"] = ai != null ? ai.summary : "";
                Session["bullets"] = ai != null ? ai.bullets : "";
                Session["radar_json"] = ai != null ? ai.radar_chart_json : BuildRadarJson(ai != null ? ai.scores : null);
                Session["bar_json"] = ai != null ? ai.bar_chart_json : "null";
                Session["pairs"] = ai != null ? ai.pairsJson : "[]";

                Response.Redirect("~/ResultDashboard.aspx?id=" + analysisId, false);
            }
            catch (Exception ex)
            {
                LblStatus.Text = "Analysis failed: " + ex.Message;
            }
        }

        private long SaveAnalysis(string resumeText, string jdText, string jobTitle,
                                  decimal score, string verdict, int keywordsHit, ScoreBlock s)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings[ConnStrName].ConnectionString))
            using (var cmd = new SqlCommand("dbo.sp_SaveFullAnalysis", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", DBNull.Value);
                cmd.Parameters.AddWithValue("@ResumeText", resumeText);
                cmd.Parameters.AddWithValue("@JobTitle", jobTitle);
                cmd.Parameters.AddWithValue("@JdText", jdText);
                cmd.Parameters.AddWithValue("@CompanyName", DBNull.Value);
                cmd.Parameters.AddWithValue("@OverallScore", score);
                cmd.Parameters.AddWithValue("@Verdict", verdict);
                cmd.Parameters.AddWithValue("@KeywordsHit", keywordsHit);
                cmd.Parameters.AddWithValue("@UseOpenAI", true);
                cmd.Parameters.AddWithValue("@EnableRerank", false);
                cmd.Parameters.AddWithValue("@SkillsScore", s != null ? s.Skills : 0);
                cmd.Parameters.AddWithValue("@ExperienceScore", s != null ? s.Experience : 0);
                cmd.Parameters.AddWithValue("@EducationScore", s != null ? s.Education : 0);
                cmd.Parameters.AddWithValue("@KeywordsScore", s != null ? s.Keywords : 0);
                cmd.Parameters.AddWithValue("@AtsScore", s != null ? s.ATS : 0);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return Convert.ToInt64(result);
            }
        }

        private string VerdictFromScore(decimal score)
        {
            if (score >= 75) return "Ready";
            if (score >= 55) return "Improve";
            return "Major Gap";
        }

        private string GuessJobTitle(string jd)
        {
            if (string.IsNullOrWhiteSpace(jd)) return "Unknown Role";
            string firstLine = jd.Split('\n')[0].Trim();
            return firstLine.Length > 40 ? firstLine.Substring(0, 40) + "..." : firstLine;
        }

        private string BuildRadarJson(ScoreBlock s)
        {
            if (s == null)
                return @"{""type"":""radar"",""data"":{""labels"":[""Skills"",""Experience"",""Education"",""Keywords"",""ATS""],
                        ""datasets"":[{""label"":""Match"",""data"":[70,70,70,70,70],""fill"":true}]}}";
            return JsonConvert.SerializeObject(new
            {
                type = "radar",
                data = new
                {
                    labels = new[] { "Skills", "Experience", "Education", "Keywords", "ATS" },
                    datasets = new[] {
                        new { label = "Match", data = new [] { s.Skills, s.Experience, s.Education, s.Keywords, s.ATS }, fill = true }
                    }
                }
            });
        }

        class AnalysisResponse
        {
            public decimal score { get; set; }
            public string verdict { get; set; }
            public int? keywords_hit { get; set; }
            public string job_title { get; set; }
            public ScoreBlock scores { get; set; }
            public string summary { get; set; }
            public string bullets { get; set; }
            public string radar_chart_json { get; set; }
            public string bar_chart_json { get; set; }
            public string pairsJson { get; set; }
        }

        class ScoreBlock
        {
            public decimal Skills { get; set; }
            public decimal Experience { get; set; }
            public decimal Education { get; set; }
            public decimal Keywords { get; set; }
            public decimal ATS { get; set; }
        }
    }
}
