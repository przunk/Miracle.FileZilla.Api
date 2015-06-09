﻿using System;
using System.IO;

namespace Miracle.FileZilla.Api
{
    internal class Error: IBinarySerializable
    {
        public bool IsError { get; set; }
        public string Message { get; set; }

        public void Deserialize(BinaryReader reader, int protocolVersion)
        {
            IsError = reader.ReadBoolean();
            Message = reader.ReadRemainingAsText();
        }

        public void Serialize(BinaryWriter writer, int protocolVersion)
        {
            throw new NotImplementedException();
        }
    }
}