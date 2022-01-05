using System.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using Fjord;
using Fjord.Modules.Camera;
using Fjord.Modules.Game;
using Fjord.Modules.Graphics;
using Fjord.Modules.Input;
using Fjord.Modules.Mathf;
using Fjord.Modules.Debug;
using Fjord.Modules.Ui;

namespace Glacier {
    class glacier_format {
        public List<property>? properties;
        public V2? grid_size;
        public V2? tile_size;
        public Dictionary<string, tile>? tiles;
        public List<List<Dictionary<string, dynamic>>>? tile_map;
    }
}