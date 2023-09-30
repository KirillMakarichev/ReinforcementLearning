using System.ComponentModel;
using System.Text;
using Timer = System.Windows.Forms.Timer;

namespace Snake;

public partial class Form1 : Form
{
    private const int BlockSize = 20;
    private float _snakeSpeed = 1;
    private const int FieldWidth = 20 * BlockSize;
    private const int FieldHeight = 20 * BlockSize;

    private readonly Color _backgroundColor = Color.Black;
    private readonly Color _snakeColor = Color.Green;
    private readonly Color _headColor = Color.OliveDrab;
    private readonly Color _foodColor = Color.Red;
    private readonly Font _font = new(FontFamily.GenericSansSerif, 12, FontStyle.Regular, GraphicsUnit.Pixel);

    private Timer _gameTimer;

    private bool _paused = false; // Variable to track the game's paused state

    private Game1 _game;

    private Graphics _graphics = new();

// Add this method to handle the pause/resume functionality
    private void TogglePause()
    {
        _paused = !_paused; // Toggle the paused state

        if (_paused)
        {
            _gameTimer.Stop(); // Stop the game timer
        }
        else
        {
            _gameTimer.Start(); // Resume the game timer
        }
    }

    public Form1()
    {
        InitializeComponent();

        this.Load += OnLoad;
        Closing += OnClosing;
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        await _game.SaveAsync(
            $"{DateTime.UtcNow.ToString("MM/dd/yyyy_hh_mm_ss")}_score_{_game.BrainStatistic().MaxScore}.txt");
    }

    private async void OnLoad(object? sender, EventArgs e)
    {
        KeyDown += MainForm_KeyDown;
        KeyPress += Form1_KeyPress;
        _graphics.Show();
        // Set up the game window
        Width = 900;
        Height = 700;
        _game = Factories.CreateGame(FieldWidth / BlockSize, FieldHeight / BlockSize);
        BackColor = _backgroundColor;

        // Set up the game timer
        _gameTimer = new Timer();
        _gameTimer.Interval = (int)(1000 / _snakeSpeed);
        _gameTimer.Tick += GameTick;
        _gameTimer.Start();
    }

    private void Form1_KeyPress(object sender, KeyPressEventArgs e)
    {
        if (e.KeyChar == '=' || e.KeyChar == '+')
            SetSpeed(_snakeSpeed * 1.1f);
        else if (e.KeyChar == '-' || e.KeyChar == '_')
            SetSpeed(_snakeSpeed / 1.1f);
    }

    private void SetSpeed(float speed)
    {
        this._snakeSpeed = speed;
        var interval = 100f / speed;
        _gameTimer.Interval = (int)interval;
        // if (interval >= 1)
        //     frameSkip = 0;
        // else
        //     frameSkip = (int) (speed / 100);
    }

    bool _isPc = true;

    private void MainForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            TogglePause();
            return;
        }

        if (e.KeyCode == Keys.H)
        {
            _isPc = !_isPc;
            return;
        }

        // Handle keyboard input to change the snake's direction
        Direction nextDirection;
        switch (e.KeyCode)
        {
            case Keys.Left:
                nextDirection = Direction.Left;
                break;
            case Keys.Right:
                nextDirection = Direction.Right;
                break;
            case Keys.Up:
                nextDirection = Direction.Up;
                break;
            case Keys.Down:
                nextDirection = Direction.Down;
                break;
            default:
                return;
        }

        //_game.SetNextDirection(nextDirection);
    }

    private void GameTick(object sender, EventArgs e)
    {
        if (_paused) return;

        _game.Tick(withFuturePossibleStates: false);

        // Refresh the game window
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.DrawRectangle(new Pen(Color.White), new Rectangle(0, 0, FieldWidth, FieldHeight));
        // Draw the snake
        var snakeBody = _game.SnakeBody.Select(x => Pos.ConvertFromPosToPoint(x, BlockSize)).ToList();
        foreach (var block in snakeBody.Skip(1).ToList())
        {
            e.Graphics.FillRectangle(new SolidBrush(_snakeColor), block.X, block.Y, BlockSize, BlockSize);
        }

        var headPoint = snakeBody[0];
        e.Graphics.FillRectangle(new SolidBrush(_headColor), headPoint.X, headPoint.Y, BlockSize, BlockSize);

        // Draw the food
        foreach (var food in _game.FoodLocation)
        {
            var foodLocation = Pos.ConvertFromPosToPoint(food, BlockSize);
            var solidBrush = food == _game.TargetFood ? new SolidBrush(Color.Blue) : new SolidBrush(_foodColor);

            e.Graphics.FillRectangle(solidBrush, foodLocation.X, foodLocation.Y, BlockSize, BlockSize);
        }

        // Draw the walls
        foreach (var wall in _game.Walls)
        {
            var wallLocation = Pos.ConvertFromPosToPoint(wall, BlockSize);
            var solidBrush = new SolidBrush(Color.Gray);

            e.Graphics.FillRectangle(solidBrush, wallLocation.X, wallLocation.Y, BlockSize, BlockSize);
        }

        // Draw the score
        var scoreText = $"Score: {_game.Score}";
        e.Graphics.DrawString(scoreText, _font, Brushes.White, new Point(10, 10));

        var statistics = _game.BrainStatistic();

        var statisticsStringBuilder = new StringBuilder();

        // if (statistics.Sensors != null)
        // {
        //     statisticsStringBuilder.AppendJoin('\n',
        //         statistics.Sensors.Select((x, i) => $"{i % 8}){x} - {Pos.Dir8[i % 8]}"));
        // }

        if (_game.SensorsAdditionalInfo.Any())
        {
            var dict = new Dictionary<int, Color>()
            {
                { 0, Color.Azure }, { 1, Color.Chartreuse }, { 2, Color.Fuchsia }, { 3, Color.Teal }
            };
            foreach (var sensor in _game.SensorsAdditionalInfo)
            {
                e.Graphics.DrawLine(new Pen(dict[sensor.Item1]), GetCenterOfBlock(headPoint),
                    GetCenterOfBlock(Pos.ConvertFromPosToPoint(sensor.Item2, BlockSize)));
            }
        }

        statisticsStringBuilder.AppendLine();
        statisticsStringBuilder.AppendLine();

        statisticsStringBuilder.AppendLine($"food hungry {statistics.FoodHungry}");
        statisticsStringBuilder.AppendLine($"reward {statistics.Reward}");
        statisticsStringBuilder.AppendLine($"exploration {statistics.Exploration}");
        statisticsStringBuilder.AppendLine($"food collected {statistics.FoodCollected}");
        statisticsStringBuilder.AppendLine($"max score {statistics.MaxScore}");

        statisticsStringBuilder.AppendLine();
        statisticsStringBuilder.AppendLine();

        if (statistics.QValues != null)
        {
            var max = statistics.QValues.Max();
            for (var i = 0; i < statistics.QValues.Count; i++)
            {
                var brush = statistics.QValues[i] == max ? Brushes.Chartreuse : Brushes.White;

                e.Graphics.DrawString($"{statistics.QValues[i]} - {(Direction)i}",
                    _font, brush,
                    new Point(_game.Field.Width * BlockSize + 100, 10 + i * 20));
            }
        }

        statisticsStringBuilder.AppendLine();
        statisticsStringBuilder.AppendLine();

        statisticsStringBuilder.AppendLine($"food {_game.TargetFood}");
        statisticsStringBuilder.AppendLine($"head {_game.SnakeBody[0]}");

        e.Graphics.DrawString(statisticsStringBuilder.ToString(), _font, Brushes.White,
            new Point(_game.Field.Width * BlockSize + 10, 10));

        _graphics.AddErrors(_game.ErrorsP);
    }

    private Point GetCenterOfBlock(Point point)
    {
        point.X += BlockSize / 2;
        point.Y += BlockSize / 2;

        return point;
    }
}

// Enum to represent the snake's direction