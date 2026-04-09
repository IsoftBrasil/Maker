using System.Drawing;
using System.Windows.Forms;

namespace PrintAgent.Services;

public sealed class AgentTrayContext : ApplicationContext
{
    private readonly Func<Task> _onExitAsync;
    private readonly NotifyIcon _trayIcon;

    public AgentTrayContext(Func<Task> onExitAsync)
    {
        _onExitAsync = onExitAsync;

        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("Maker Print Agent ativo") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Fechar agente", null, OnExitClicked));

        _trayIcon = new NotifyIcon
        {
            Text = "Maker Print Agent",
            Icon = SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) =>
        {
            _trayIcon.ShowBalloonTip(
                2500,
                "Maker Print Agent",
                "Agente em execução. Clique com o botão direito para fechar.",
                ToolTipIcon.Info);
        };

        _trayIcon.ShowBalloonTip(
            2500,
            "Maker Print Agent",
            "Agente iniciado com sucesso.",
            ToolTipIcon.Info);
    }

    private async void OnExitClicked(object? sender, EventArgs e)
    {
        try
        {
            await _onExitAsync();
        }
        finally
        {
            ExitThread();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
