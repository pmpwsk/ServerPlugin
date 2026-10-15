using uwap.WebFramework.Responses.DefaultUI;

namespace uwap.WebFramework.Plugins;

public partial class ServerPlugin
{
    private class MemoryCollector(string name, Func<long> getter)
    {
        public string Name = name;
        public Func<long> Getter = getter;
        public List<Graph.DataPoint> History = [];
    }
    
    private List<MemoryCollector> MemoryCollectors =
    [
        new("Proc.WorkingSet64", () => System.Diagnostics.Process.GetCurrentProcess().WorkingSet64),
        new("Proc.PrivateMemorySize64", () => System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64),
        new("GC.HeapSizeBytes", () => GC.GetGCMemoryInfo().HeapSizeBytes),
        new("GC.TotalCommittedBytes", () => GC.GetGCMemoryInfo().TotalCommittedBytes),
        new("GC.FragmentedBytes", () => GC.GetGCMemoryInfo().FragmentedBytes),
        new("GC.TotalMemory", () => GC.GetTotalMemory(false))
    ];
    
    private DateTime FirstMemoryDate = DateTime.UtcNow;
    
    private string[] MonthNames =
    [
        "January",
        "February",
        "March",
        "April",
        "May",
        "June",
        "July",
        "August",
        "September",
        "October",
        "November",
        "December"
    ];
    
    private string DateFormat(DateTime date)
        => $"{MonthNames[date.Month - 1]} {date.Day}, {date.Year}";
    
    private void CollectMemory()
    {
        if (EnableMemory && (DateTime.UtcNow - FirstMemoryDate).TotalDays >= MemoryCollectors.First().History.Count)
        {
            foreach (var memoryCollector in MemoryCollectors)
            {
                var value = memoryCollector.Getter() / 1024 / 1024;
                memoryCollector.History.Add(new(Convert.ToDouble(value), $"{value} MB on {DateFormat(DateTime.UtcNow)}"));
            }
        }
    }

    [Endpoint("/memory")]
    protected Page HandleMemory(Request req)
    {
        req.ForceGET();
        
        var page = new Page(req, true, "Memory usage");
        page.Sections.Add(new Section(
            "Memory usage",
            [
                ..MemoryCollectors.Select(memoryCollector => CreateMemorySubsection(page, memoryCollector)),
                new Subsection(
                    "Actions",
                    [
                        new ServerActionButton(
                            "GC.Collect()",
                            _ => GC.Collect()
                        )
                    ]
                )
            ]
        ));
        
        return page;
    }
    
    private static Subsection CreateMemorySubsection(Page page, MemoryCollector memoryCollector)
    {
        var size = memoryCollector.History.Count;
        var subsection = new Subsection(
            memoryCollector.Name,
            [
                new Graph(memoryCollector.History).Save(out var graph),
                new Paragraph("Current: ...").Save(out var currentText)
            ]
        );
        Task.Run(async () =>
        {
            while (!page.CancellationOnDispose.IsCancellationRequested)
            {
                var value = memoryCollector.Getter() / 1024 / 1024;
                currentText.SetLines($"Current: {value} MB");
                
                if (memoryCollector.History.Count > size)
                {
                    graph.SetValues(memoryCollector.History);
                    size = memoryCollector.History.Count;
                }
                
                await Task.Delay(250);
            }
        });
        return subsection;
    }
}