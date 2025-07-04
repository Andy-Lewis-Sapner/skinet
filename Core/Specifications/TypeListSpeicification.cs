using System;
using Core.Entities;

namespace Core.Specifications;

public class TypeListSpeicification : BaseSpecification<Product, string>
{
    public TypeListSpeicification()
    {
        AddSelect(x => x.Type);
        ApplyDistinct();
    }
}
