using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Nito.AsyncEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;

namespace DotNetInteractivePSCmdlet
{
    [Cmdlet(VerbsLifecycle.Invoke, "Kernel")]
    public class InvokeKernelCommand : Cmdlet
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ValueFromRemainingArguments = true, Position = 1)]
        [Alias("Code", "Contents")]
        public string InputObject;

        [Parameter(ValueFromPipelineByPropertyName = true, Position = 2)]
        [Alias("Language")]
        public string Kernel = "pwsh";

        private readonly Kernel kernel;
        //private readonly Repl repl;
        public InvokeKernelCommand()
        {
            var pwsh = new PowerShellKernel()
                                .UseProfiles()
                                .UseDotNetVariableSharing();

            //var csharp = new CSharpKernel()
            //                    .UseNugetDirective()
            //                    .UseKernelHelpers()
            //                    .UseWho();

            var fsharp = new FSharpKernel()
                                .UseDefaultFormatting()
                                .UseNugetDirective()
                                .UseKernelHelpers()
                                .UseWho();

            var kernel = new CompositeKernel {
                pwsh,
                //csharp,
                fsharp,
            };
            kernel.DefaultKernelName = pwsh.Name;
            this.kernel = kernel;

            Formatter.SetPreferredMimeTypeFor(typeof(object), "text/plain");
            Formatter.Register<object>(o => o.ToString());
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var targetCmd = new SubmitCode(InputObject);
            WriteInformation("RunKernelCommand: " + targetCmd, new []{"PSHOST"});
            var task = Task.Run(async () =>
            {
                var result = await kernel.SendAsync(targetCmd);
                var waiter = result.KernelEvents.ToSubscribedList();
                return waiter;
            });
            var events = task.GetAwaiter().GetResult();
            WriteInformation("Got events!", new []{"PSHOST"});

            WriteObject(events.OfType<DisplayEvent>().ToArray());

        }
    }

    public static class ObservableExtensions
    {
        public static SubscribedList<T> ToSubscribedList<T>(this IObservable<T> source)
        {
            return new SubscribedList<T>(source);
        }
    }

    public class SubscribedList<T> : IReadOnlyList<T>, IDisposable
    {
        private ImmutableArray<T> _list = ImmutableArray<T>.Empty;
        private readonly IDisposable _subscription;

        public SubscribedList(IObservable<T> source)
        {
            _subscription = source.Subscribe(x => { _list = _list.Add(x); });
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _list.Length;

        public T this[int index] => _list[index];

        public void Dispose() => _subscription.Dispose();
    }
}