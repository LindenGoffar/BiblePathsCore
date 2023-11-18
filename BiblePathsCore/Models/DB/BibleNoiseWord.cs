using System;
using System.Collections.Generic;

namespace BiblePathsCore.Models.DB;

public partial class BibleNoiseWord
{
    public string BibleId { get; set; }

    public string NoiseWord { get; set; }

    public int Occurs { get; set; }

    public bool IsNoise { get; set; }

    public int WordType { get; set; }

    public virtual Bible Bible { get; set; }
}
