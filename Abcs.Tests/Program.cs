using System.Threading.Tasks;

namespace Abcs.Tests;

public class Program
{
	public static async Task Main(string[] args)
	{
		App app = new App();
		await app.Start();
	}
}