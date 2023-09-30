namespace Snake;

public class Field
{
    private const int DefaultWidth = 40;
    private const int DefaultHeight = 40;

    public int Width { get; }
    public int Height { get; }

    public Field() : this(DefaultWidth, DefaultHeight)
    {
    }

    public Field(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public bool Contains(Pos pos) =>
        pos.Y >= 0 && pos.Y < Height && pos.X >= 0 && pos.X < Width;

    public static List<Pos> GetFreeCells(Field field, List<Pos> taken)
    {
        var freeCells = new List<Pos>();

        for (var row = 0; row < field.Height; row++)
        {
            for (var col = 0; col < field.Width; col++)
            {
                var cell = new Pos(row, col);
            
                // Проверяем, является ли позиция свободной и не находится ли она в списке занятых позиций
                if (!taken.Contains(cell))
                {
                    freeCells.Add(cell);
                }
            }
        }

        return freeCells;
    }

    public static List<Pos> GetRandomFreeCells(Field field, List<Pos> taken, int count)
    {
        return GetFreeCells(field, taken).OrderBy(x => Random.Shared.Next()).Take(count).ToList();
    }
}