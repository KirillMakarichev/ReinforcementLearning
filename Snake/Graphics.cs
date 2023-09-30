using System.Windows.Forms.DataVisualization.Charting;
using Timer = System.Windows.Forms.Timer;

namespace Snake;

public partial class Graphics : Form
{
    private Timer _timer;
    private Chart _chart;

    public Graphics()
    {
        InitializeComponent();
    }

    public void AddErrors(List<double> errors)
    {
        if (errors == null || !errors.Any()) return;

        if (_chart == null)
        {
            ConfigureChart(errors);
        }

        AddPoints(errors);
    }

    private int _y = 0;

    private void ConfigureChart(List<double> errors)
    {
        var knownColors = Enum.GetValues<KnownColor>();
        var colorsCount = knownColors.Length;
        var random = new Random(111);

        var c = new Chart();
        var count = errors.Count;
        c.Dock = DockStyle.Fill;

        for (var i = 0; i < count; i++)
        {
            var s = new Series($"error {i}");
            s.ChartType = SeriesChartType.Line;

            s.Color = Color.FromKnownColor(knownColors[random.Next(0, colorsCount)]);
            c.Series.Add(s);

            var area = c.ChartAreas.Add(c.ChartAreas.NextUniqueName());
            s.ChartArea = area.Name;
        }

        _chart = c;
        this.Controls.Add(c);
    }

    private void AddPoints(List<double> errors)
    {
        for (var index = 0; index < errors.Count; index++)
        {
            var error = errors[index];
            _chart.Series[index].Points.AddXY(_y, error);
        }

        _y++;
    }
}