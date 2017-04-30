﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace OmniSharp.Roslyn.CSharp.Services.Intellisense
{
    internal static class CompletionItemExtensions
    {
        private static MethodInfo _getSymbolsAsync;

        static CompletionItemExtensions()
        {
            var symbolCompletionItemType = typeof(CompletionItem).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.Completion.Providers.SymbolCompletionItem");
            _getSymbolsAsync = symbolCompletionItemType.GetMethod("GetSymbolsAsync", BindingFlags.Public | BindingFlags.Static);
        }

        public static async Task<IEnumerable<ISymbol>> GetCompletionSymbols(this CompletionItem completionItem, IEnumerable<ISymbol> recommendedSymbols, Document document)
        {
            // for SymbolCompletionProvider, use the logic of extracting information from recommended symbols
            if (completionItem.Properties.ContainsKey("Provider") && completionItem.Properties["Provider"] == "Microsoft.CodeAnalysis.CSharp.Completion.Providers.SymbolCompletionProvider")
            {
                var symbols = recommendedSymbols.Where(x => x.Name == completionItem.Properties["SymbolName"] && (int)x.Kind == int.Parse(completionItem.Properties["SymbolKind"])).Distinct();
                return symbols ?? Enumerable.Empty<ISymbol>();
            }

            // if the completion provider encoded symbols into Properties, we can return them
            if (completionItem.Properties.ContainsKey("Symbols"))
            {
                // the API to decode symbols is not public at the moment
                // http://source.roslyn.io/#Microsoft.CodeAnalysis.Features/Completion/Providers/SymbolCompletionItem.cs,93
                var decodedSymbolsTask = _getSymbolsAsync.Invoke(null, new object[] { completionItem, document, default(CancellationToken) }) as Task<ImmutableArray<ISymbol>>;
                if (decodedSymbolsTask != null)
                {
                    return await decodedSymbolsTask;
                }
            }

            return Enumerable.Empty<ISymbol>();
        }
    }
}
