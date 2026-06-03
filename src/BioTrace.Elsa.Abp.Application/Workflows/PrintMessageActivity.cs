using System;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace BioTrace.Elsa.Abp.Workflows;

[Activity("BioTrace", "Samples", "Prints a message to the console.")]
public class PrintMessageActivity : CodeActivity
{
    [Input(Description = "The message to print.")]
    public Input<string> Message { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var message = Message.Get(context);
        Console.WriteLine(message);
    }
}
