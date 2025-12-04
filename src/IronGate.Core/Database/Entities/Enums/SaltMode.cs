using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronGate.Core.Database.Entities.Enums;
public enum SaltMode {
    None = 0,
    Global = 1,
    PerUser = 2
}