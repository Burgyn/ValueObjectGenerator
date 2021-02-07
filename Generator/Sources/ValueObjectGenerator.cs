using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SGFramework;
using SGFramework.TypeDeclaration;

namespace ValueObjectGenerator
{
    [Generator]
    internal class ValueObjectGenerator : SourceGenerator<TypeDeclarationSyntaxReceiver>, IAttributeContainsChecker
    {
        private static readonly AttributeTypeName ValueObjectAttributeTypeName = new ( "ValueObject" );
        private static readonly AttributeTypeName RangeAttributeTypeName = new ( "Range" );
        private static readonly AttributeTypeName NonNegativeAttributeTypeName = new ( "NonNegative" );

        public override TypeDeclarationSyntaxReceiver CreateSyntaxReceiver()
            => new( this );

        public override void SetupAttributeArgumentParser( Dictionary<AttributeTypeName, IAttributeArgumentParser> map )
        {
            map[ ValueObjectAttributeTypeName ] = new ValueObjectAttributeArgumentParser();
            map[ RangeAttributeTypeName ]       = new RangeAttributeArgumentParser();
            map[ NonNegativeAttributeTypeName ] = new EmptyAttributeArgumentParser();
        }

        public bool ContainsAttribute( AttributeTypeName attributeTypeName ) =>
            attributeTypeName == ValueObjectAttributeTypeName ||
            attributeTypeName == RangeAttributeTypeName ||
            attributeTypeName == NonNegativeAttributeTypeName;

        public override void GenerateAttributeCode( GeneratorExecutionContext context )
        {
            context.AddSource( ValueObjectAttributeTypeName.Value, new ValueObjectAttributeTemplate().TransformText() );
            context.AddSource( RangeAttributeTypeName.Value,       new RangeAttributeTemplate().TransformText() );
            context.AddSource( NonNegativeAttributeTypeName.Value, new NonNegativeAttributeTemplate().TransformText() );
        }

        protected override string GenerateCode(
            TypeDeclarationSyntax declaration,
            string nameSpace,
            string typeName,
            IDictionary<AttributeTypeName, IDictionary<AttributeParamName, object>> attributeTypeList )
        {

            #region Get Attributes
            if( !attributeTypeList.TryGetValue( ValueObjectAttributeTypeName, out var valueObjectAttributeParams ))
            {
                // "ValueObject" is Require attribute
                return string.Empty;
            }
            if( !attributeTypeList.TryGetValue( RangeAttributeTypeName, out var rangeAttributeParams ))
            {
                // "ValueObject" is NOT Require attribute
                rangeAttributeParams = null!;
            }
            if( !attributeTypeList.TryGetValue( NonNegativeAttributeTypeName, out var nonNegativeAttributeParams ))
            {
                // "ValueObject" is NOT Require attribute
                nonNegativeAttributeParams = null!;
            }
            #endregion

            #region ValueObjectAttribute
            if( !valueObjectAttributeParams.TryGetValue( AttributeParameterNames.BaseName, out var baseName ) )
            {
                return string.Empty;
            }

            if( !valueObjectAttributeParams.TryGetValue( AttributeParameterNames.ValueName, out var valueName ) )
            {
                valueName = "Value";
            }
            if( !valueObjectAttributeParams.TryGetValue( AttributeParameterNames.OptionFlags, out var valueOption ) )
            {
                valueOption = ValueOption.None;
            }
            #endregion

            #region RangeAttribute
            object minValue = string.Empty;
            object maxValue = string.Empty;
            if( rangeAttributeParams != null! )
            {
                if( !rangeAttributeParams.TryGetValue( AttributeParameterNames.Min, out minValue ) )
                {
                    minValue = string.Empty;
                }
                if( !rangeAttributeParams.TryGetValue( AttributeParameterNames.Max, out maxValue ) )
                {
                    maxValue = string.Empty;
                }
            }
            #endregion

            var template = new ValueObjectTemplate()
            {
                Namespace    = nameSpace,
                IsClass      = declaration is ClassDeclarationSyntax,
                IsStruct     = declaration is StructDeclarationSyntax,
                Name         = typeName,
                BaseTypeName = (string)baseName,
                ValueName    = valueName.ToString(),
                ValueOption  = (ValueOption)valueOption,
                Min          = minValue.ToString(),
                Max          = maxValue.ToString(),
                NonNegative  = nonNegativeAttributeParams != null
            };

            var code = template.TransformText();

            return code;
        }
    }
}
