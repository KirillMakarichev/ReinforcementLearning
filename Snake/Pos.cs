namespace Snake;

public struct Pos
{
    public static IReadOnlyList<Pos> Dir4 { get; } = new[]
    {
        new Pos(-1, 0), new Pos(0, 1), new Pos(1, 0), new Pos(0, -1)
    };

    public static IReadOnlyList<Pos> Dir8 { get; } = new[]
    {
        new Pos(-1, 0), new Pos(-1, 1), new Pos(0, 1), new Pos(1, 1),
        new Pos(1, 0), new Pos(1, -1), new Pos(0, -1), new Pos(-1, -1)
    };


    public int Y { get; set; }
    public int X { get; set; }

    public Pos(int row, int col)
    {
        Y = row;
        X = col;
    }

    public override bool Equals(object obj) =>
        obj is Pos that && this.Y == that.Y && this.X == that.X;

    public override int GetHashCode() => (Row: Y, Col: X).GetHashCode();

    public override string ToString() => $"({Y}r, {X}c)";

    public static bool operator ==(Pos a, Pos b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Pos a, Pos b) => !a.Equals(b);

    public static Pos operator +(Pos a, Pos b) => new Pos(a.Y + b.Y, a.X + b.X);
    public static Pos operator *(int n, Pos a) => new Pos(n * a.Y, n * a.X);

    public static Pos ConvertFromPoint(Point point, int scalingFactor)
    {
        return new Pos(point.Y / scalingFactor + 1, point.X / scalingFactor + 1);
    }

    public static Point ConvertFromPosToPoint(Pos pos, int scalingFactor)
    {
        //return new Point((pos.Col - 1) * scalingFactor + scalingFactor / 2, (pos.Row - 1) * scalingFactor + scalingFactor / 2);
        return new Point(pos.X * scalingFactor, pos.Y * scalingFactor);
    }

    public static double Distance(Pos pos1, Pos pos2)
    {
        return Math.Sqrt(Math.Pow(pos1.X - pos2.X, 2) + Math.Pow(pos1.Y - pos2.Y, 2));
    }
}