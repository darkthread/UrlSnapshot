using System.Text.RegularExpressions;
using UrlSnapshot;

try
{
    if (!args.Any()) throw new ApplicationException("url list filepath required");
    var urlList = File.ReadAllLines(args[0])
        .Where(line => !string.IsNullOrEmpty(line))
        .Select(line =>
        {
            if (line.StartsWith('#')) return null;
            var p = line.Split(',', '\t');
            if (p.Length != 2) 
                throw new ApplicationException($"invalid data: {line} {p.Length}");
            return new Job { Title = p[0], Url = p[1] };
        }).Where(o => o != null).ToArray();
    // get Chrome installed path from registry (Windows only)
    var path = Microsoft.Win32.Registry.GetValue(
        @"HKEY_CLASSES_ROOT\ChromeHTML\shell\open\command", null, null) as string;
    if (string.IsNullOrEmpty(path))
        throw new ApplicationException("Chrome not installed");
    var m = Regex.Match(path, "\"(?<p>.+?)\"");
    if (!m.Success)
        throw new ApplicationException($"Invalid Chrome path - {path}");
    var chromePath = m.Groups["p"].Value;
    // prepare result folder, use date time as folder name
    var resultFolder = Path.Combine(".", "Results", DateTime.Now.ToString("MMdd-HHmm"));
    Directory.CreateDirectory(resultFolder);

    using (var browser = new Chrome(chromePath))
    {
        foreach (var job in urlList)
        {
            browser.Navigate(job);
            if (job.Pass) 
                browser.TakeSnapshot(Path.Combine(resultFolder, job.Title + ".png"));
            else 
                File.WriteAllText(Path.Combine(resultFolder, job.Title + ".txt"), job.Message);
            Console.WriteLine($"{(job.Pass ? "SUCC" : "FAIL")} {job.Url}");
        }
    }

}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR - {ex.Message}");
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine(ex.ToString());
    Console.ResetColor();
    return;
}