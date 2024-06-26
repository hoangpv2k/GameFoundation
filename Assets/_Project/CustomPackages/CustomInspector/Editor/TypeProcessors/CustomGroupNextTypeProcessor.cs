﻿using System;
using System.Collections.Generic;
using CustomInspector;
using CustomInspector.TypeProcessors;
using CustomInspector.Utilities;

[assembly: RegisterCustomTypeProcessor(typeof(CustomGroupNextTypeProcessor), 11000)]

namespace CustomInspector.TypeProcessors
{
    public class CustomGroupNextTypeProcessor : CustomTypeProcessor
    {
        public override void ProcessType(Type type, List<CustomPropertyDefinition> properties)
        {
            CustomPropertyDefinition groupProperty = null;
            GroupAttribute groupAttribute = null;
            TabAttribute tabAttribute = null;

            foreach (var property in properties)
            {
                if (groupProperty != null)
                {
                    if (groupProperty.OwnerType != property.OwnerType ||
                        groupProperty.Order + 1000 < property.Order)
                    {
                        groupProperty = null;
                        groupAttribute = null;
                        tabAttribute = null;
                    }
                }

                if (property.Attributes.TryGet(out GroupNextAttribute groupNextAttribute))
                {
                    groupProperty = property;

                    groupAttribute = groupNextAttribute.Path != null
                        ? new GroupAttribute(groupNextAttribute.Path)
                        : null;

                    property.Attributes.TryGet(out tabAttribute);
                }

                if (groupAttribute != null && !property.Attributes.TryGet(out GroupAttribute _))
                {
                    property.GetEditableAttributes().Add(groupAttribute);
                }

                if (tabAttribute != null && !property.Attributes.TryGet(out TabAttribute _))
                {
                    property.GetEditableAttributes().Add(tabAttribute);
                }
            }
        }
    }
}