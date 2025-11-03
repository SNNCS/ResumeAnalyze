<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="History.aspx.cs" Inherits="ResumeAnalyze.History" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Analysis History</title>
    <meta charset="utf-8" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/@picocss/pico@2/css/pico.min.css" />
    <style>
        .container { max-width: 1100px; margin: 24px auto; }
        .card { padding: 18px; border-radius: 12px; background: #fff; box-shadow: 0 6px 18px rgba(0,0,0,.08); }
        .toolbar { display:flex; gap:10px; align-items:center; margin-bottom: 12px; }
        .muted { color:#666; font-size:.92rem; }
        .table { width:100%; border-collapse: collapse; }
        .table th, .table td { padding:10px 12px; border-bottom:1px solid #eee; text-align:left; }
        .badge { padding:4px 8px; border-radius:10px; background:#e2e8f0; }
        .ok { background:#d1fae5; }
        .warn { background:#fde68a; }
        .bad { background:#fecaca; }
    </style>
</head>
<body>
    <form id="form1" runat="server" class="container">
        <asp:ScriptManager runat="server" ID="ScriptManager1" />
        <div class="toolbar">
            <asp:HyperLink ID="LnkBack" runat="server" NavigateUrl="~/UploadResume.aspx" Text="← 返回上传页" />
            <asp:Button ID="BtnRefresh" runat="server" Text="刷新" OnClick="BtnRefresh_Click" />
            <asp:TextBox ID="TxtSearch" runat="server" Placeholder="按职位 / 公司 / 时间筛选" Width="320" />
            <asp:Button ID="BtnSearch" runat="server" Text="搜索" OnClick="BtnSearch_Click" />
            <asp:Label ID="LblInfo" runat="server" CssClass="muted" />
        </div>

        <div class="card">
            <h3>历史分析记录</h3>
            <asp:GridView ID="GridHistory" runat="server" AutoGenerateColumns="False" CssClass="table"
                OnRowDataBound="GridHistory_RowDataBound">
                <Columns>
                    <asp:BoundField DataField="CreatedAt" HeaderText="时间" DataFormatString="{0:yyyy-MM-dd HH:mm}" />
                    <asp:BoundField DataField="JobTitle" HeaderText="职位" />
                    <asp:BoundField DataField="Company" HeaderText="公司" />
                    <asp:TemplateField HeaderText="总分">
                        <ItemTemplate>
                            <span class="badge"><%# Eval("Score") %></span>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="判定">
                        <ItemTemplate>
                            <span runat="server" id="VerdictBadge" class="badge">--</span>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="操作">
                        <ItemTemplate>
                            <asp:HyperLink runat="server" Text="查看" NavigateUrl='<%# Eval("ResultUrl") %>' />
                            &nbsp;|&nbsp;
                            <asp:HyperLink runat="server" Text="导出" NavigateUrl='<%# Eval("ReportUrl") %>' />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                    <span class="muted">暂无记录。</span>
                </EmptyDataTemplate>
            </asp:GridView>
        </div>
    </form>
</body>
</html>
