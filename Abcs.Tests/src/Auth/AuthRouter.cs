namespace Abcs.Tests;

using Abcs.Http;

public class AuthRouter : HttpRouter
{
	public AuthRouter()
	{
		UseRouteMatching();
		MapGet("/register", AuthController.RegisterGet);
		MapPost("/register", AuthController.RegisterPost);
		MapGet("/login", AuthController.LoginGet);
		MapPost("/login", AuthController.LoginPost);
		MapGet("/logout", AuthController.LogoutGet);
		MapPost("/logout", AuthController.LogoutPost);
	}
}
