using System;
using System.Reflection;
using Abcs.Utils;
using Xunit;

public class CIOTests
{
	// ---------- Helpers ----------

	private static string[,,] GetBuffer(CIO cio)
	{
		var field = typeof(CIO).GetField("buffer",
				BindingFlags.NonPublic | BindingFlags.Instance);
		Assert.NotNull(field);

		var value = field!.GetValue(cio);
		Assert.NotNull(value);

		return (string[,,]) value!;
	}

	private static (int rows, int cols, int layers) GetDimensions(CIO cio)
	{
		var rowsField = typeof(CIO).GetField("rows",
				BindingFlags.NonPublic | BindingFlags.Instance);
		var colsField = typeof(CIO).GetField("cols",
				BindingFlags.NonPublic | BindingFlags.Instance);
		var layersField = typeof(CIO).GetField("layers",
				BindingFlags.NonPublic | BindingFlags.Instance);

		Assert.NotNull(rowsField);
		Assert.NotNull(colsField);
		Assert.NotNull(layersField);

		int rows = (int) rowsField!.GetValue(cio)!;
		int cols = (int) colsField!.GetValue(cio)!;
		int layers = (int) layersField!.GetValue(cio)!;

		return (rows, cols, layers);
	}

	private static (int row, int col, int layer) GetCursor(CIO cio)
	{
		var rowField = typeof(CIO).GetField("cursorRow",
				BindingFlags.NonPublic | BindingFlags.Instance);
		var colField = typeof(CIO).GetField("cursorCol",
				BindingFlags.NonPublic | BindingFlags.Instance);
		var layerField = typeof(CIO).GetField("cursorLayer",
				BindingFlags.NonPublic | BindingFlags.Instance);

		Assert.NotNull(rowField);
		Assert.NotNull(colField);
		Assert.NotNull(layerField);

		int row = (int) rowField!.GetValue(cio)!;
		int col = (int) colField!.GetValue(cio)!;
		int layer = (int) layerField!.GetValue(cio)!;

		return (row, col, layer);
	}

	// ---------- Constructor ----------

	[Fact]
	public void Constructor_InitializesBufferAndDefaults()
	{
		var cio = new CIO(rows: 10, cols: 20, layers: 2);

		var (rows, cols, layers) = GetDimensions(cio);
		Assert.Equal(10, rows);
		Assert.Equal(20, cols);
		Assert.Equal(2, layers);

		var buffer = GetBuffer(cio);
		Assert.Equal(2, buffer.GetLength(0));
		Assert.Equal(10, buffer.GetLength(1));
		Assert.Equal(20, buffer.GetLength(2));

		var (cursorRow, cursorCol, cursorLayer) = GetCursor(cio);
		Assert.Equal(0, cursorRow);
		Assert.Equal(0, cursorCol);
		Assert.Equal(0, cursorLayer);
	}

	// ---------- ConsoleColorToAnsiForeground ----------

	[Theory]
	[InlineData(ConsoleColor.Black, "\x1b[30m")]
	[InlineData(ConsoleColor.DarkRed, "\x1b[31m")]
	[InlineData(ConsoleColor.DarkGreen, "\x1b[32m")]
	[InlineData(ConsoleColor.DarkYellow, "\x1b[33m")]
	[InlineData(ConsoleColor.DarkBlue, "\x1b[34m")]
	[InlineData(ConsoleColor.DarkMagenta, "\x1b[35m")]
	[InlineData(ConsoleColor.DarkCyan, "\x1b[36m")]
	[InlineData(ConsoleColor.Gray, "\x1b[37m")]
	[InlineData(ConsoleColor.DarkGray, "\x1b[90m")]
	[InlineData(ConsoleColor.Red, "\x1b[91m")]
	[InlineData(ConsoleColor.Green, "\x1b[92m")]
	[InlineData(ConsoleColor.Yellow, "\x1b[93m")]
	[InlineData(ConsoleColor.Blue, "\x1b[94m")]
	[InlineData(ConsoleColor.Magenta, "\x1b[95m")]
	[InlineData(ConsoleColor.Cyan, "\x1b[96m")]
	[InlineData(ConsoleColor.White, "\x1b[97m")]
	public void ConsoleColorToAnsiForeground_MapsKnownColors(ConsoleColor color, string expected)
	{
		var result = CIO.ConsoleColorToAnsiForeground(color);
		Assert.Equal(expected, result);
	}

	[Fact]
	public void ConsoleColorToAnsiForeground_NullOrUnknown_ReturnsReset()
	{
		Assert.Equal(CIO.RESET, CIO.ConsoleColorToAnsiForeground(null));

		// Unknown value
		var unknown = (ConsoleColor) 9999;
		Assert.Equal(CIO.RESET, CIO.ConsoleColorToAnsiForeground(unknown));
	}

	// ---------- ConsoleColorToAnsiBackground ----------

	[Theory]
	[InlineData(ConsoleColor.Black, "\x1b[40m")]
	[InlineData(ConsoleColor.DarkRed, "\x1b[41m")]
	[InlineData(ConsoleColor.DarkGreen, "\x1b[42m")]
	[InlineData(ConsoleColor.DarkYellow, "\x1b[43m")]
	[InlineData(ConsoleColor.DarkBlue, "\x1b[44m")]
	[InlineData(ConsoleColor.DarkMagenta, "\x1b[45m")]
	[InlineData(ConsoleColor.DarkCyan, "\x1b[46m")]
	[InlineData(ConsoleColor.Gray, "\x1b[47m")]
	[InlineData(ConsoleColor.DarkGray, "\x1b[100m")]
	[InlineData(ConsoleColor.Red, "\x1b[101m")]
	[InlineData(ConsoleColor.Green, "\x1b[102m")]
	[InlineData(ConsoleColor.Yellow, "\x1b[103m")]
	[InlineData(ConsoleColor.Blue, "\x1b[104m")]
	[InlineData(ConsoleColor.Magenta, "\x1b[105m")]
	[InlineData(ConsoleColor.Cyan, "\x1b[106m")]
	[InlineData(ConsoleColor.White, "\x1b[107m")]
	public void ConsoleColorToAnsiBackground_MapsKnownColors(ConsoleColor color, string expected)
	{
		var result = CIO.ConsoleColorToAnsiBackground(color);
		Assert.Equal(expected, result);
	}

	[Fact]
	public void ConsoleColorToAnsiBackground_NullOrUnknown_ReturnsReset()
	{
		Assert.Equal(CIO.RESET, CIO.ConsoleColorToAnsiBackground(null));

		var unknown = (ConsoleColor) 9999;
		Assert.Equal(CIO.RESET, CIO.ConsoleColorToAnsiBackground(unknown));
	}

	// ---------- Color(char) ----------

	[Fact]
	public void Color_Char_WithForegroundAndBackground()
	{
		char ch = 'A';
		var fg = ConsoleColor.Red;
		var bg = ConsoleColor.Blue;

		var result = CIO.Color(ch, fg, bg);

		var expected = CIO.ConsoleColorToAnsiForeground(fg)
									 + CIO.ConsoleColorToAnsiBackground(bg)
									 + ch
									 + CIO.RESET;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void Color_Char_WithNullColors_UsesResets()
	{
		char ch = 'Z';

		var result = CIO.Color(ch, null, null);

		var expected = CIO.RESET + CIO.RESET + ch + CIO.RESET;
		Assert.Equal(expected, result);
	}

	// ---------- Color(string) ----------

	[Fact]
	public void Color_String_WithForegroundAndBackground()
	{
		string text = "Hello";
		var fg = ConsoleColor.Green;
		var bg = ConsoleColor.Black;

		var result = CIO.Color(text, fg, bg);

		var expected = CIO.ConsoleColorToAnsiForeground(fg)
									 + CIO.ConsoleColorToAnsiBackground(bg)
									 + text
									 + CIO.RESET;

		Assert.Equal(expected, result);
	}

	[Fact]
	public void Color_String_WithNullColors_UsesResets()
	{
		string text = "Test";

		var result = CIO.Color(text, null, null);

		var expected = CIO.RESET + CIO.RESET + text + CIO.RESET;
		Assert.Equal(expected, result);
	}

	// ---------- Write basics ----------

	[Fact]
	public void Write_SingleChar_UsesCurrentCursorAndUpdatesBuffer()
	{
		var cio = new CIO(rows: 3, cols: 3, layers: 1);

		cio.Write("A");

		var buffer = GetBuffer(cio);
		string? stored = buffer[0, 0, 0];

		var expected = CIO.Color('A', ConsoleColor.Black, ConsoleColor.Black);
		Assert.Equal(expected, stored);

		var (row, col, layer) = GetCursor(cio);
		Assert.Equal(0, row);
		Assert.Equal(1, col);   // moved one column ahead
		Assert.Equal(0, layer);
	}

	[Fact]
	public void Write_SecondCall_WithoutRowCol_UsesUpdatedCursor()
	{
		var cio = new CIO(rows: 3, cols: 3, layers: 1);

		cio.Write("A"); // [0,0]
		cio.Write("B"); // [0,1]

		var buffer = GetBuffer(cio);

		Assert.Equal(
				CIO.Color('A', ConsoleColor.Black, ConsoleColor.Black),
				buffer[0, 0, 0]);

		Assert.Equal(
				CIO.Color('B', ConsoleColor.Black, ConsoleColor.Black),
				buffer[0, 0, 1]);

		var (row, col, layer) = GetCursor(cio);
		Assert.Equal(0, row);
		Assert.Equal(2, col);
		Assert.Equal(0, layer);
	}

	[Fact]
	public void Write_WithExplicitRowColLayer_WritesToSpecifiedLocation()
	{
		var cio = new CIO(rows: 5, cols: 5, layers: 3);

		cio.Write("X", row: 2, col: 3, layer: 1, fg: ConsoleColor.Yellow, bg: ConsoleColor.Red);

		var buffer = GetBuffer(cio);
		string? stored = buffer[1, 2, 3];

		Assert.Equal(
				CIO.Color('X', ConsoleColor.Yellow, ConsoleColor.Red),
				stored);

		var (row, col, layer) = GetCursor(cio);
		Assert.Equal(2, row);
		Assert.Equal(4, col);  // wrote at col 3, moved to col 4
		Assert.Equal(1, layer);
	}

	[Fact]
	public void Write_RespectsLayerParameter()
	{
		var cio = new CIO(rows: 2, cols: 2, layers: 2);

		cio.Write("L", layer: 1);

		var buffer = GetBuffer(cio);

		Assert.Null(buffer[0, 0, 0]); // base layer untouched
		Assert.Equal(
				CIO.Color('L', ConsoleColor.Black, ConsoleColor.Black),
				buffer[1, 0, 0]);
	}

	// ---------- Write wrapping and newline handling ----------

	[Fact]
	public void Write_WrapsToNextRow_WhenColumnLimitReached()
	{
		var cio = new CIO(rows: 2, cols: 2, layers: 1);

		// cols = 2, so positions: [0,0], [0,1], wrap, [1,0], [1,1]
		cio.Write("ABCD", fg: ConsoleColor.White, bg: ConsoleColor.Black);

		var buffer = GetBuffer(cio);

		Assert.Equal(CIO.Color('A', ConsoleColor.White, ConsoleColor.Black), buffer[0, 0, 0]);
		Assert.Equal(CIO.Color('B', ConsoleColor.White, ConsoleColor.Black), buffer[0, 0, 1]);
		Assert.Equal(CIO.Color('C', ConsoleColor.White, ConsoleColor.Black), buffer[0, 1, 0]);
		Assert.Equal(CIO.Color('D', ConsoleColor.White, ConsoleColor.Black), buffer[0, 1, 1]);
	}

	[Fact]
	public void Write_NewlineMovesToNextRowAndStoresNewlineChar()
	{
		var cio = new CIO(rows: 3, cols: 2, layers: 1);

		// "AB\nC"
		cio.Write("AB\nC", fg: ConsoleColor.Cyan, bg: ConsoleColor.Black);

		var buffer = GetBuffer(cio);

		Assert.Equal(CIO.Color('A', ConsoleColor.Cyan, ConsoleColor.Black), buffer[0, 0, 0]);
		Assert.Equal(CIO.Color('B', ConsoleColor.Cyan, ConsoleColor.Black), buffer[0, 0, 1]);

		// According to implementation: on '\n' it increments row and resets col,
		// then writes the '\n' character at the new position.
		Assert.Equal(CIO.Color('\n', ConsoleColor.Cyan, ConsoleColor.Black), buffer[0, 1, 0]);
		Assert.Equal(CIO.Color('C', ConsoleColor.Cyan, ConsoleColor.Black), buffer[0, 1, 1]);
	}

	[Fact]
	public void Write_CarriageReturnIsSkipped()
	{
		var cio = new CIO(rows: 1, cols: 3, layers: 1);

		cio.Write("A\rB", fg: ConsoleColor.Magenta, bg: ConsoleColor.Black);

		var buffer = GetBuffer(cio);

		Assert.Equal(CIO.Color('A', ConsoleColor.Magenta, ConsoleColor.Black), buffer[0, 0, 0]);
		Assert.Equal(CIO.Color('B', ConsoleColor.Magenta, ConsoleColor.Black), buffer[0, 0, 1]);
		Assert.Null(buffer[0, 0, 2]); // nothing written for '\r'
	}

	// ---------- Default colors in Write ----------

	[Fact]
	public void Write_DefaultColors_UseBlackForegroundAndBackground()
	{
		var cio = new CIO(rows: 1, cols: 1, layers: 1);

		// fg/bg parameters in Write are non-nullable ConsoleColor with default value,
		// so default(...) => ConsoleColor.Black
		cio.Write("X");

		var buffer = GetBuffer(cio);
		var stored = buffer[0, 0, 0];

		var expected = CIO.Color('X', ConsoleColor.Black, ConsoleColor.Black);
		Assert.Equal(expected, stored);
	}
}
