using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhelanB.RemoveBackground.Models
{
    public class ModelOptions
    {
        public int InputWidth { get; set; } = 320;
        public int InputHeight { get; set; } = 320;

        public int OutputWidth { get; set; } = 320;
        public int OutputHeight { get; set; } = 320;

        public string InputParamater { get; set; } = "input_image";
        public ModelOptions() { }
    }
}
