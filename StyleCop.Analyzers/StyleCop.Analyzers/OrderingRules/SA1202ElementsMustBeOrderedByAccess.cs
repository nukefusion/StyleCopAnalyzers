namespace StyleCop.Analyzers.OrderingRules
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    /// <summary>
    /// An element within a C# code file is out of order within regard to access level, in relation to other elements in
    /// the code.
    /// </summary>
    /// <remarks>
    /// <para>A violation of this rule occurs when the code elements within a file do not follow a standard ordering
    /// scheme based on access level.</para>
    ///
    /// <para>To comply with this rule, adjacent elements of the same type must be positioned in the following order by
    /// access level:</para>
    ///
    /// <list type="bullet">
    /// <item>public</item>
    /// <item>internal</item>
    /// <item>protected internal</item>
    /// <item>protected</item>
    /// <item>private</item>
    /// </list>
    ///
    /// <para>Complying with a standard ordering scheme based on access level can increase the readability and
    /// maintainability of the file and make it easier to identify the public interface that is being exposed from a
    /// class.</para>
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SA1202ElementsMustBeOrderedByAccess : DiagnosticAnalyzer
    {
        /// <summary>
        /// The ID for diagnostics produced by the <see cref="SA1202ElementsMustBeOrderedByAccess"/> analyzer.
        /// </summary>
        public const string DiagnosticId = "SA1202";
        private const string Title = "Elements must be ordered by access";
        private const string MessageFormat = "All {0} {2} must be placed after all {1} {2}";
        private const string Description = "An element within a C# code file is out of order within regard to access level, in relation to other elements in the code.";
        private const string HelpLink = "http://www.stylecop.com/docs/SA1202.html";

        private static readonly DiagnosticDescriptor Descriptor =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, AnalyzerCategory.OrderingRules, DiagnosticSeverity.Warning, AnalyzerConstants.DisabledNoTests, Description, HelpLink);

        private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsValue =
            ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return SupportedDiagnosticsValue;
            }
        }

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            // TODO: Implement analysis
            context.RegisterSyntaxNodeActionHonorExclusions(this.HandleType, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }

        private void HandleType(SyntaxNodeAnalysisContext context)
        {
            this.HandleMembersOfType<FieldDeclarationSyntax>(context, "fields");
            this.HandleMembersOfType<MethodDeclarationSyntax>(context, "methods");
            this.HandleMembersOfType<DelegateDeclarationSyntax>(context, "delegates");
        }

        private void HandleMembersOfType<T>(SyntaxNodeAnalysisContext context, string memberType) where T : MemberDeclarationSyntax
        {
            var typeDeclaration = context.Node as TypeDeclarationSyntax;
            if (typeDeclaration == null)
            {
                return;
            }

            var data = typeDeclaration.Members.OfType<T>().Select(this.GetModifierData).ToList();
            var accessModifiers = data.Select(x => x.Priority).Distinct().ToList();
            for (int i = 0; i < data.Count - 1; i++)
            {
                var thisData = data[i];
                var nextData = data[i + 1];
                if (nextData.Priority < thisData.Priority)
                {
                    var location = thisData.Modifers.Any()
                        ? thisData.Modifers.First().GetLocation()
                        : thisData.Node.GetLocation();

                    var diagnostic = Diagnostic.Create(
                        Descriptor,
                        location,
                        this.GetModifierText(thisData.Priority),
                        this.GetModifierText(this.GetNextLowestPriorityFromPresentTokens(thisData, accessModifiers)),
                        memberType);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private struct DeclarationModifierData
        {
            public ModifierPriority Priority { get; set; }

            public SyntaxTokenList Modifers { get; set; }

            public SyntaxNode Node { get; set; }
        }

        private DeclarationModifierData GetModifierData(SyntaxNode node)
        {
            var modifiers = GetModifiersFromDeclaration(node);
            return new DeclarationModifierData
            {
                Node = node,
                Modifers = modifiers,
                Priority = this.GetAccessModifierOrderPriority(modifiers)
            };
        }

        private string GetModifierText(ModifierPriority priority)
        {
            switch (priority)
            {
                case ModifierPriority.Public:
                    return "public";
                case ModifierPriority.Internal:
                    return "internal";
                case ModifierPriority.ProtectedInternal:
                    return "protected internal";
                case ModifierPriority.Protected:
                    return "protected";
                default:
                    return "private";
            }
        }

        private ModifierPriority GetNextLowestPriorityFromPresentTokens(DeclarationModifierData current, List<ModifierPriority> data)
        {
            ModifierPriority next = ModifierPriority.Public;
            for (int index = 0; index < data.Count; index++)
            {
                ModifierPriority priority = data[index];
                if (priority > next && priority < current.Priority)
                {
                    next = priority;
                }
            }

            return next;
        }

        /// <summary>
        /// Defines the access modifiers in priority order.
        /// </summary>
        private enum ModifierPriority
        {
            Public,

            Internal,

            ProtectedInternal,

            Protected,

            Private,
        }

        /// <summary>
        /// Given a token list containing access modifier tokens, returns a <see cref="ModifierPriority"/> that
        /// describes an order for the access modifier.
        /// </summary>
        /// <param name="modifierTokens">A token list containing the access modifier tokens.</param>
        /// <returns>The priority order.</returns>
        private ModifierPriority GetAccessModifierOrderPriority(SyntaxTokenList modifierTokens)
        {
            if (modifierTokens.Count == 0)
            {
                return ModifierPriority.Private;
            }

            var firstKind = modifierTokens.First().Kind();
            if (modifierTokens.Count == 2)
            {
                var secondKind = modifierTokens[1].Kind();
                if (firstKind == SyntaxKind.ProtectedKeyword && secondKind == SyntaxKind.InternalKeyword)
                {
                    return ModifierPriority.ProtectedInternal;
                }
            }

            switch (firstKind)
            {
                case SyntaxKind.PublicKeyword:
                    return ModifierPriority.Public;
                case SyntaxKind.InternalKeyword:
                    return ModifierPriority.Internal;
                case SyntaxKind.ProtectedKeyword:
                    return ModifierPriority.Protected;
                case SyntaxKind.PrivateKeyword:
                    return ModifierPriority.Private;
            }

            return ModifierPriority.Private;
        }

        // TODO: Copied from SA1206 - extract into standalone helper.
        private static SyntaxTokenList GetModifiersFromDeclaration(SyntaxNode node)
        {
            SyntaxTokenList result = default(SyntaxTokenList);

            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                    result = ((BaseTypeDeclarationSyntax)node).Modifiers;
                    break;
                case SyntaxKind.EnumDeclaration:
                    result = ((EnumDeclarationSyntax)node).Modifiers;
                    break;
                case SyntaxKind.DelegateDeclaration:
                    result = ((DelegateDeclarationSyntax)node).Modifiers;
                    break;
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    result = ((BaseFieldDeclarationSyntax)node).Modifiers;
                    break;
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    result = ((BasePropertyDeclarationSyntax)node).Modifiers;
                    break;
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                    result = ((BaseMethodDeclarationSyntax)node).Modifiers;
                    break;
            }

            return result;
        }
    }
}
