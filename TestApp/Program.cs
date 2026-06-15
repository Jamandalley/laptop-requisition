using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        using var client = new HttpClient();
        
        // Test with just Username
        var json = "{\"Username\": \"test\"}";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync("https://sso-app-dev.digitvant.com/api/users/initiate-reset", content);
        var body = await response.Content.ReadAsStringAsync();
        
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Body: {body}");
        
        // Test with Email if Username fails
        var json2 = "{\"Email\": \"test@example.com\"}";
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");
        
        var response2 = await client.PostAsync("https://sso-app-dev.digitvant.com/api/users/initiate-reset", content2);
        var body2 = await response2.Content.ReadAsStringAsync();
        
        Console.WriteLine($"Status2: {response2.StatusCode}");
        Console.WriteLine($"Body2: {body2}");
    }
}
