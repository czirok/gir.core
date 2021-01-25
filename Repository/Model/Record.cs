﻿using Repository.Analysis;

namespace Repository.Model
{
    public record Record : ISymbol
    {
        public Namespace Namespace { get; init; }
        public string ManagedName { get; set; }
        public string NativeName { get; init; }
        
        public TypeReference? GLibClassStructFor { get; init; }
    }
}
