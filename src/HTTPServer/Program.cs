using System.Net;
using System.Net.Sockets;
using System.Text;

public static class HttpStatusCode {
  public const string Ok = "200 OK";
  public const string BadRequest = "400 Bad Request";
  public const string Unauthorized = "401 Unauthorized";
  public const string Forbidden = "403 Forbidden";
  public const string NotFound = "404 Not Found";
  public const string InternalServerError = "500 Internal Server Error";
  public const string NotImplemented = "501 Not Implemented";
  public const string BadGateway = "502 Bad Gateway";
  public const string ServiceUnavailable = "503 Service Unavailable";
  public const string GatewayTimeout = "504 Gateway Timeout";
}

public static class MyTcpServer
{
    record HttpRequest(string Method, string Path, string Version, string Host, string TwoNumbers);
    const string NEW_LINE = "\r\n";

    private static void Main(string[] args) {

        // Create the TCP Server
        TcpListener server = new TcpListener(IPAddress.Any, 4321);

        // Start the TCP Server
        server.Start();
        Console.WriteLine("Behold! The server started!");
        
        // Old: Wait for client to connect to Socket
        // var socket = server.AcceptSocket();

        // New: Wait for client to connect to TcpClient
        while (true) {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("A new connection accepted!");

            // For each request, asynchronously call HandleRequest() on it
            Task.Run(() => HandleRequest(client));
        }
    }

    private static void HandleRequest(TcpClient client) {

        Stream stream = client.GetStream();
        HttpRequest request = ReadRequest(stream);
        Console.WriteLine("Request read!");

        // Old: Simply send a 200 OK Response
        // string ackMessage = "HTTP/1.1 200 OK\r\n\r\n";
        // socket.Send(System.Text.Encoding.UTF8.GetBytes(ackMessage));

        // New: Advanced request-processing
        byte[] response = ProcessRequest(request);
        Console.WriteLine("Request processed!");

        stream.Write(response, 0, response.Length);
        Console.WriteLine("Message sent!");

        client.Close();
        Console.WriteLine("Connection closed!");
    }

    private static HttpRequest ReadRequest(Stream stream) {

        // Create a requestBuffer (byte array)
        byte[] requestBuffer = new byte[1024];                                  

        // Old: Extract the request from the Socket Connection
        // int numBytes = socket.Receive(requestBuffer, SocketFlags.None);      // Store the incoming message in the requestBuffer

        // New: Extract the request from the TcpClient Connection
        int numBytes = stream.Read(requestBuffer);                              // Store the incoming message in the requestBuffer

        // Convert the requestBuffer to a string
        string[] requestLines = System.Text.Encoding.UTF8.GetString(requestBuffer, 0, numBytes).Split(NEW_LINE);

        // Read the first line (Request Line)
        string[] requestLine = requestLines[0].Split(' ');                      // Split the request line by space
        (string requestMethod, string requestPath, string requestVersion) = (requestLine[0], requestLine[1], requestLine[2]);

        // Read the remaining lines (Request Header & Body)
        string? host = null;
        string? twoNumbers = null;

        for (int i = 1; i < requestLines.Length; i++) {
            string line = requestLines[i];

            if (line.StartsWith("Host", StringComparison.OrdinalIgnoreCase)) {
                host = GetHeaderValue(line);
            } else if (line.StartsWith("Two-Numbers", StringComparison.OrdinalIgnoreCase)) {
                twoNumbers = GetHeaderValue(line);
            }
        }

        return new HttpRequest(requestMethod, requestPath, requestVersion, host, twoNumbers);
    }

    private static string GetHeaderValue(string line) { return line.Split(": ")[1]; }

    private static byte[] ProcessRequest(HttpRequest request) {

        string statusCode = HttpStatusCode.InternalServerError;
        byte[]? responseBody = null;
        string contentType = "plain/text";

        // Old: Generate simple response message
        // responseBody = path == "/" ? "HTTP/1.1 200 OK\r\n"
        //                               : "HTTP/1.1 404 Not Found\r\n";

        // New: Generate advanced response message
        if (request.Method == "GET")
        {
            if (request.Path == @"/")
            {
                statusCode = HttpStatusCode.Ok;
            }
            else if (request.Path == @"/get-sum/") 
            {
                statusCode = HttpStatusCode.Ok;
                
                // Calculate the sum of the TwoNumbers passed in the request header
                string[] twoNumbers = request.TwoNumbers.Split(',');
                int sum = Int32.Parse(twoNumbers[0]) + Int32.Parse(twoNumbers[1]);
                string requestArgument = sum.ToString();
                responseBody = System.Text.Encoding.UTF8.GetBytes(requestArgument);
            }
            else if (request.Path.StartsWith(@"/echo/"))
            {
                statusCode = HttpStatusCode.Ok;

                string requestArgument = request.Path.Substring(6);
                responseBody = System.Text.Encoding.UTF8.GetBytes(requestArgument);
            }
            else if (request.Path.StartsWith(@"/site/"))
            {
                string[] requestArgument = request.Path.Substring(6).Split('/');
                string? requestDomain;
                string? requestAsset;
                string? requestFilePath = null;
                Console.WriteLine($"Requesting site: {request.Path}");

                if (requestArgument.Length == 1)
                {
                    requestDomain = requestArgument[0];
                    requestAsset = "index.html";
                    requestFilePath = @$"./wwwroot/{requestDomain}/{requestAsset}";
                }
                else if (requestArgument.Length == 2)
                {
                    requestDomain = requestArgument[0];
                    requestAsset = requestArgument[1];
                    requestFilePath = @$"./wwwroot/{requestDomain}/{requestAsset}";
                }

                // Check if the requested site exists in the wwwroot directory
                if (requestFilePath != null && File.Exists(requestFilePath))
                {
                    // Serve requested HTML file
                    statusCode = HttpStatusCode.Ok;
                    Console.WriteLine($"Host: {request.Host}");
                    Console.WriteLine($"Requesting file: {requestFilePath}");

                    string fileToServe = File.ReadAllText(requestFilePath);
                    responseBody = System.Text.Encoding.UTF8.GetBytes(fileToServe);

                    // Set content type based on file extension.
                    string ext = Path.GetExtension(requestFilePath).ToLower();
                    if (ext == ".css")
                    {
                        contentType = "text/css";
                    }
                    else if (ext == ".html")
                    {
                        contentType = "text/html";
                    }

                }
                else
                {
                    statusCode = HttpStatusCode.NotFound;
                }
            }
            else
            {
                statusCode = HttpStatusCode.BadRequest;
            }
        }
        else
        {
            statusCode = HttpStatusCode.Forbidden;
        }

        return WriteResponse(statusCode, responseBody, contentType);
    }

    private static byte[] WriteResponse(string statusCode, byte[]? body, string contentType) {
        // Create a message array to hold the response header and body
        List<byte> message = new List<byte>();
        StringBuilder responseHeader = new StringBuilder();

        // Add the Status Line to the responseHeader array
        responseHeader.Append($"HTTP/1.1 {statusCode}");
        responseHeader.Append(NEW_LINE);

        // Add the Headers to the responseHeader array
        if (body != null) {
            responseHeader.Append($"Content-Type: {contentType}");
            responseHeader.Append(NEW_LINE);
            responseHeader.Append($"Content-Length: {body.Length}");
            responseHeader.Append(NEW_LINE);
        }
        responseHeader.Append(NEW_LINE);
        message.AddRange(Encoding.UTF8.GetBytes(responseHeader.ToString()));

        // Add the Body to the responseHeader array
        if (body != null) {
            message.AddRange(body);
        }

        return message.ToArray();
    }
}


/* To test the server, you can use the following command ------------------------------------------------------------ */

// curl -v http://localhost:4321
// curl -v http://localhost:4321/
// curl -v http://localhost:4321/echo/
// curl -v http://localhost:4321/echo/abc
// curl -v http://localhost:4321/get-sum/ --header "Two-Numbers: 1,2"
    // (optional) --header "Host: localhost:4321" --header "Accept: */*"


/* Advanced ways to use CURL ------------------------------------------------------------ */

// To send a POST request with data:
// curl -X POST -d "key=value" http://localhost:4321

// To send a JSON payload with a POST request:
// curl -X POST -H "Content-Type: application/json" -H "Authorization: Bearer <token>" -d '{"key":"value"}' http://localhost:4321

// To save the response to a file:
// curl -o response.html http://localhost:4321

// To test with a file upload:
// curl -X POST -F "file=@path/to/file.txt" http://localhost:4321

// To test with verbose output for debugging:
// curl -v http://localhost:4321

// To test with a timeout:
// curl --max-time 10 http://localhost:4321