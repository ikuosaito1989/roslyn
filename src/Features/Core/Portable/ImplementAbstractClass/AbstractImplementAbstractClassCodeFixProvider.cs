﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementAbstractClass
{
    internal abstract class AbstractImplementAbstractClassCodeFixProvider<TClassNode> : CodeFixProvider
        where TClassNode : SyntaxNode
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; }

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        protected AbstractImplementAbstractClassCodeFixProvider(string diagnosticId)
            => FixableDiagnosticIds = ImmutableArray.Create(diagnosticId);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var token = root.FindToken(context.Span.Start);
            if (!token.Span.IntersectsWith(context.Span))
                return;

            var classNode = token.Parent.GetAncestorOrThis<TClassNode>();
            if (classNode == null)
                return;

            var data = await ImplementAbstractClassData.TryGetDataAsync(document, classNode, cancellationToken).ConfigureAwait(false);
            if (data == null)
                return;

            var abstractClassType = data.AbstractClassType;
            var id = GetCodeActionId(abstractClassType.ContainingAssembly.Name, abstractClassType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            context.RegisterCodeFix(
                new MyCodeAction(
                    FeaturesResources.Implement_abstract_class,
                    c => data.ImplementAbstractClassAsync(c), id),
                context.Diagnostics);
        }

        private static string GetCodeActionId(string assemblyName, string abstractTypeFullyQualifiedName)
            => FeaturesResources.Implement_abstract_class + ";" + assemblyName + ";" + abstractTypeFullyQualifiedName;

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string id)
                : base(title, createChangedDocument, id)
            {
            }
        }
    }
}
