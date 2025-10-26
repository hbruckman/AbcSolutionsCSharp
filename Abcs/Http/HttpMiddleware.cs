using System.Collections;
using System.Net;

namespace Abcs.Http;

public delegate Task HttpMiddleware(HttpListenerRequest req, HttpListenerResponse res, Hashtable props, Func<Task> next);
