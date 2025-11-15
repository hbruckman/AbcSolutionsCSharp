using System.Runtime.InteropServices;

namespace Abcs.Utils;

public class ConsoleIO
{
	public const string SINGLE_BORDER_PARTS = "─│┌┐└┘";
	public const string DOUBLE_BORDER_PARTS = "═║╔╗╚╝";

	public const string SINGLE_BORDER_PARTS_WITH_CONNECTORS = "─│┌┐└┘┴┬┤├";
	public const string DOUBLE_BORDER_PARTS_WITH_CONNECTORS = "═║╔╗╚╝╩╦╣╠";

	private static int rows = 25;
	private static int cols = 80;
	private static int layers = 1;

	private static int cursorRow = 0;
	private static int cursorCol = 0;
	private static int cursorLayer = 0;

	private static string[,,] buffer = new string[layers, rows, cols];

	public static void Set(int rows = -1, int cols = -1, int layers = -1)
	{
		ConsoleIO.cols = (cols < 0) ? ConsoleIO.cols : cols;
		ConsoleIO.rows = (rows < 0) ? ConsoleIO.rows : rows;
		ConsoleIO.layers = (layers < 0) ? ConsoleIO.layers : layers;
		buffer = new string[ConsoleIO.layers, rows, cols];
	}

	public static void SetCols(int cols)
	{
		ConsoleIO.cols = (cols < 0) ? ConsoleIO.cols : cols;
		buffer = new string[layers, rows, ConsoleIO.cols];
	}

	public static void SetRows(int rows)
	{
		ConsoleIO.rows = (rows < 0) ? ConsoleIO.rows : rows;
		buffer = new string[layers, ConsoleIO.rows, cols];
	}

	public static void SetLayers(int layers)
	{
		ConsoleIO.layers = (layers < 0) ? ConsoleIO.layers : layers;
		buffer = new string[ConsoleIO.layers, rows, cols];
	}

	public static string[] GetBorder(string borderParts, ConsoleColor? fg, ConsoleColor? bg)
	{
		string[] border = new string[10];

		if(string.IsNullOrEmpty(borderParts))
		{
			Array.Fill(border, Color(string.Empty, fg, bg));
		}
		else if(borderParts.Length == 1)
		{
			Array.Fill(border, Color(borderParts, fg, bg));
		}
		else if(borderParts.Length >= 2)
		{
			string hl = Color(borderParts[0], fg, bg);
			string vl = Color(borderParts[1], fg, bg);

			Array.Fill(border, hl);
			border[1] = vl;
			border[8] = vl;
			border[9] = vl;

			if(borderParts.Length >= 6)
			{
				border[2] = Color(borderParts[2], fg, bg);
				border[3] = Color(borderParts[3], fg, bg);
				border[4] = Color(borderParts[4], fg, bg);
				border[5] = Color(borderParts[5], fg, bg);
			}

			if(borderParts.Length >= 10)
			{
				border[6] = Color(borderParts[6], fg, bg);
				border[7] = Color(borderParts[7], fg, bg);
				border[8] = Color(borderParts[8], fg, bg);
				border[9] = Color(borderParts[9], fg, bg);
			}
		}

		return border;
	}

	public static void Write(string text, int row = -1, int col = -1, int layer = -1, ConsoleColor? fg = default, ConsoleColor? bg = default)
	{
		text = text.Replace("\r", "");

		if(row < 0) { row = cursorRow; }
		if(col < 0) { col = cursorCol; }
		if(layer < 0) { layer = cursorLayer; }

		cursorRow = row;
		cursorCol = col;
		cursorLayer = layer;

		for(int i = 0; i < text.Length; i++)
		{
			char c = text[i];

			if(c == '\n' || cursorCol >= cols) { cursorRow++; cursorCol = 0; }

			buffer[layer, cursorRow, cursorCol++] = Color(c, fg, bg);
		}
	}

	public static void Box(string text, int boxRows = -1, int boxCols = -1, int layer = -1, ConsoleColor? fg = default, ConsoleColor? bg = default, float hAlign = 0.5F, float vAlign = 0.5F, string borderParts = SINGLE_BORDER_PARTS)
	{
		text = text.Replace("\r", "");

		var lines = Wrap(text, boxRows, boxCols);
		int maxRowCount = (boxRows > 0) ? boxRows : lines.Count;
		int maxColCount = (boxCols > 0) ? boxCols : lines.Max(l => l.Length);

		if(layer < 0) { layer = cursorLayer; }

		cursorLayer = layer;

		int l = (int) ((cols - maxColCount - 2) * hAlign);
		int t = (int) ((rows - maxRowCount - 2) * vAlign);
		int r = l + maxColCount + 1;
		int b = t + maxRowCount + 1;
		int c = (l + r) / 2;
		int m = (t + b) / 2;

		// Console.WriteLine($"text={text}");
		// Console.WriteLine($"text.Length={text.Length}");
		// Console.WriteLine($"lines={string.Join(", ", lines)}");
		// Console.WriteLine($"maxRowCount={maxRowCount}");
		// Console.WriteLine($"maxColCount={maxColCount}");
		// Console.WriteLine($"l={l}, t={t}, r={r}, b={b}, c={c}, m={m}");

		string[] bo = GetBorder(borderParts, fg, bg);

		for(int i = l + 1; i < r; i++)
		{
			buffer[layer, t, i] = bo[0];
			buffer[layer, b, i] = bo[0];
		}

		for(int i = t + 1; i < b; i++)
		{
			buffer[layer, i, l] = bo[1];
			buffer[layer, i, r] = bo[1];
		}

		buffer[layer, t, l] = bo[2];
		buffer[layer, t, r] = bo[3];
		buffer[layer, b, l] = bo[4];
		buffer[layer, b, r] = bo[5];
		buffer[layer, t, c] = bo[6];
		buffer[layer, b, c] = bo[7];
		buffer[layer, m, l] = bo[8];
		buffer[layer, m, r] = bo[9];

		for(int i = 0; i < lines.Count; i++)
		{
			Write(lines[i], t + 1 + i, l + 1, layer, fg, bg);
		}
	}

	public static List<string> Wrap(string text, int rowsLimit, int colsLimit)
	{
		int margin = (colsLimit < 0) ? cols - 2 : Math.Min(colsLimit, cols - 2);
		int lastIndex = 0;
		int colCount = 0;
		var lines = new List<string>();

		for(int i = 0; i < text.Length; i++)
		{
			if(text[i] == '\n') { lines.Add(text.Substring(lastIndex, i - lastIndex)); lastIndex = i + 1; colCount = 0; }
			else { colCount++; }
			if(colCount >= margin) { lines.Add(text.Substring(lastIndex, i - lastIndex)); lastIndex = i; colCount = 0; }
		}

		lines.Add(text.Substring(lastIndex));

		return lines;
	}

	public static string ConsoleColorToAnsiForeground(ConsoleColor? color)
	{
		switch(color)
		{
			case ConsoleColor.Black: return "\x1b[30m";
			case ConsoleColor.DarkRed: return "\x1b[31m";
			case ConsoleColor.DarkGreen: return "\x1b[32m";
			case ConsoleColor.DarkYellow: return "\x1b[33m";
			case ConsoleColor.DarkBlue: return "\x1b[34m";
			case ConsoleColor.DarkMagenta: return "\x1b[35m";
			case ConsoleColor.DarkCyan: return "\x1b[36m";
			case ConsoleColor.Gray: return "\x1b[37m";

			case ConsoleColor.DarkGray: return "\x1b[90m";
			case ConsoleColor.Red: return "\x1b[91m";
			case ConsoleColor.Green: return "\x1b[92m";
			case ConsoleColor.Yellow: return "\x1b[93m";
			case ConsoleColor.Blue: return "\x1b[94m";
			case ConsoleColor.Magenta: return "\x1b[95m";
			case ConsoleColor.Cyan: return "\x1b[96m";
			case ConsoleColor.White: return "\x1b[97m";

			default:
				return "\x1b[0m";  // RESET on any non-match
		}
	}

	public static string ConsoleColorToAnsiBackground(ConsoleColor? color)
	{
		switch(color)
		{
			case ConsoleColor.Black: return "\x1b[40m";
			case ConsoleColor.DarkRed: return "\x1b[41m";
			case ConsoleColor.DarkGreen: return "\x1b[42m";
			case ConsoleColor.DarkYellow: return "\x1b[43m";
			case ConsoleColor.DarkBlue: return "\x1b[44m";
			case ConsoleColor.DarkMagenta: return "\x1b[45m";
			case ConsoleColor.DarkCyan: return "\x1b[46m";
			case ConsoleColor.Gray: return "\x1b[47m";

			case ConsoleColor.DarkGray: return "\x1b[100m";
			case ConsoleColor.Red: return "\x1b[101m";
			case ConsoleColor.Green: return "\x1b[102m";
			case ConsoleColor.Yellow: return "\x1b[103m";
			case ConsoleColor.Blue: return "\x1b[104m";
			case ConsoleColor.Magenta: return "\x1b[105m";
			case ConsoleColor.Cyan: return "\x1b[106m";
			case ConsoleColor.White: return "\x1b[107m";

			default:
				return "\x1b[0m";  // RESET on any non-match
		}
	}

	public static string Color(char ch, ConsoleColor? fg = null, ConsoleColor? bg = null)
	{
		string fgc = ConsoleColorToAnsiForeground(fg);
		string bgc = ConsoleColorToAnsiBackground(bg);

		return fgc + bgc + ch + "\x1b[0m";
	}

	public static string Color(string text, ConsoleColor? fg = null, ConsoleColor? bg = null)
	{
		string fgc = ConsoleColorToAnsiForeground(fg);
		string bgc = ConsoleColorToAnsiBackground(bg);

		return fgc + bgc + text + "\x1b[0m";
	}
	public static void Draw()
	{
		// These operations are only supported on Windows; guard the call.

		if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			Console.SetWindowSize(cols + 1, rows + 1);
			Console.SetBufferSize(cols + 1, rows + 1);
		}

		Console.Clear();
		Draw(buffer);
	}

	private static void Draw(string[,,] buffer)
	{
		for(int r = 0; r < rows; r++)
		{
			for(int c = 0; c < cols; c++)
			{
				string s = buffer[0, r, c];
				Console.Write(string.IsNullOrEmpty(s) ? " " : s);
			}

			Console.WriteLine();
		}
	}
}

// My solution should not be longer than 582 lines.
