namespace StyleCop.Analyzers.Test.OrderingRules
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CodeAnalysis.Diagnostics;

    using StyleCop.Analyzers.OrderingRules;

    using TestHelper;

    using Xunit;
    using Xunit.Extensions;

    public class SA1202UnitTests : CodeFixVerifier
    {
        [Fact]
        public async Task TestEmptySourceAsync()
        {
            var testCode = string.Empty;
            await this.VerifyCSharpDiagnosticAsync(testCode, EmptyDiagnosticResults, CancellationToken.None).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestFieldOrderCorrectOrderAsync()
        {
            string testCode = @"class Foo
{ 
    public int field1;
    internal int field2;
    protected internal int field3;
    protected int field4;
    private int field5;
}";

            await this.VerifyCSharpDiagnosticAsync(testCode, EmptyDiagnosticResults, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Enumerates combinations of modifiers for adajacent members that should report a diagnostic.
        /// </summary>
        public static IEnumerable<object[]> ModifierCombinations => new[]
        {
            new object[] { null, "protected" },
            new object[] { null, "protected internal" },
            new object[] { null, "internal" },
            new object[] { null, "public" },
            new object[] { "private", "protected" },
            new object[] { "private", "protected internal" },
            new object[] { "private", "internal" },
            new object[] { "private", "public" },
            new object[] { "protected", "protected internal" },
            new object[] { "protected", "internal" },
            new object[] { "protected", "public" },
            new object[] { "protected internal", "internal" },
            new object[] { "protected internal", "public" },
            new object[] { "internal", "public" }
        };

        /// <summary>
        /// Asserts that incorrect ordering for fields is reported correctly.
        /// </summary>
        /// <param name="modifier1">The access modifier for the first field.</param>
        /// <param name="modifier2">The access modifier for the second field.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Theory]
        [MemberData(nameof(ModifierCombinations))]
        public async Task TestFieldOrderIncorrectReportsAsync(string modifier1, string modifier2)
        {
            string placeholder1 = FormatModifier(modifier1);
            string placeholder2 = FormatModifier(modifier2);

            string testCode =
$@"class Foo
{{ 
    {placeholder1}int field1;
    {placeholder2}int field2;
}}";

            var expected = this.CSharpDiagnostic().WithLocation(3, 5).WithArguments(GetModifierName(modifier1), modifier2, "fields");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Asserts that incorrect ordering for methods is reported correctly.
        /// </summary>
        /// <param name="modifier1">The access modifier for the first method.</param>
        /// <param name="modifier2">The access modifier for the second method.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Theory]
        [MemberData(nameof(ModifierCombinations))]
        public async Task TestMethodOrderIncorrectReportsAsync(string modifier1, string modifier2)
        {
            string placeholder1 = FormatModifier(modifier1);
            string placeholder2 = FormatModifier(modifier2);

            string testCode =
$@"class Foo
{{ 
    {placeholder1}void Method1()
    {{
    }}

    {placeholder2}void Method2()
    {{
    }}
}}";

            var expected = this.CSharpDiagnostic().WithLocation(3, 5).WithArguments(GetModifierName(modifier1), modifier2, "methods");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Asserts that incorrect ordering for delegates is reported correctly.
        /// </summary>
        /// <param name="modifier1">The access modifier for the first delegate.</param>
        /// <param name="modifier2">The access modifier for the second delegate.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Theory]
        [MemberData(nameof(ModifierCombinations))]
        public async Task TestDelegateOrderIncorrectReportsAsync(string modifier1, string modifier2)
        {
            string placeholder1 = FormatModifier(modifier1);
            string placeholder2 = FormatModifier(modifier2);

            string testCode =
$@"class Foo
{{ 
    {placeholder1}delegate void DoSomething1();
    {placeholder2}delegate void DoSomething2();
}}";

            var expected = this.CSharpDiagnostic().WithLocation(3, 5).WithArguments(GetModifierName(modifier1), modifier2, "delegates");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Asserts that incorrect ordering for events is reported correctly.
        /// </summary>
        /// <param name="modifier1">The access modifier for the first event.</param>
        /// <param name="modifier2">The access modifier for the second event.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Theory]
        [MemberData(nameof(ModifierCombinations))]
        public async Task TeskEventOrderIncorrectReportsAsync(string modifier1, string modifier2)
        {
            string placeholder1 = FormatModifier(modifier1);
            string placeholder2 = FormatModifier(modifier2);

            string testCode =
$@"class Foo
{{ 
    {placeholder1}event System.EventHandler<System.EventArgs> MyEvent1;
    {placeholder2}event System.EventHandler<System.EventArgs> MyEvent2;
}}";

            var expected = this.CSharpDiagnostic().WithLocation(3, 5).WithArguments(GetModifierName(modifier1), modifier2, "events");

            await this.VerifyCSharpDiagnosticAsync(testCode, expected, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs formatting for the specified modifier prior to insertion into the code template.
        /// </summary>
        /// <param name="modifier">The access modifier.</param>
        /// <returns>A string ready to insert into the code template placeholder.</returns>
        private static string FormatModifier(string modifier)
        {
            return modifier == null ? string.Empty : modifier + " ";
        }

        private static string GetModifierName(string modifier)
        {
            return modifier ?? "private";
        }

        /// <summary>
        /// Gets the C# analyzers being tested
        /// </summary>
        /// <returns>
        /// New instances of all the C# analyzers being tested.
        /// </returns>
        protected override IEnumerable<DiagnosticAnalyzer> GetCSharpDiagnosticAnalyzers()
        {
            yield return new SA1202ElementsMustBeOrderedByAccess();
        }
    }
}
