using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameAnalyzer.NodeEditor
{
    public interface INodeProcessor
    {
        string Name { get; }

        Dictionary<string, object?> Execute(Dictionary<string, object?> inputs);
    }
}
