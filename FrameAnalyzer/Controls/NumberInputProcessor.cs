using FrameAnalyzer.NodeEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameAnalyzer.Controls
{
    public class NumberInputProcessor : INodeProcessor
    {
        public string Name => "Number Input";

        public double Value { get; set; }

        public Dictionary<string, object?> Execute(Dictionary<string, object?> inputs)
        {
            return new Dictionary<string, object?>
            {
                ["Number"] = Value
            };
        }
    }
}
