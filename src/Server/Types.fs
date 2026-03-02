namespace Gambol.Server

/// Marker type for WebApplicationFactory<Program> in tests.
type Program = class end

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode
