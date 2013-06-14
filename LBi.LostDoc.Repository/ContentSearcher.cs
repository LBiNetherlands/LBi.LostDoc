/*
 * Copyright 2012 LBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LBi.LostDoc.Diagnostics;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Store;

namespace LBi.LostDoc.Repository
{
    public class ContentSearcher : IDisposable
    {
        private readonly StandardAnalyzer _analyzer;
        private readonly FSDirectory _directory;
        private readonly string _indexPath;
        private readonly IndexReader _indexReader;
        private readonly IndexSearcher _indexSearcher;

        public ContentSearcher(string indexPath)
        {
            this._indexPath = indexPath;
            this._directory = FSDirectory.Open(new DirectoryInfo(indexPath));
            this._indexReader = IndexReader.Open(this._directory, readOnly: true);
            this._analyzer = new StandardAnalyzer(global::Lucene.Net.Util.Version.LUCENE_29);
            this._indexSearcher = new IndexSearcher(this._indexReader);
        }

        ~ContentSearcher()
        {
            this.Dispose(false);
        }

        public string IndexPath
        {
            get { return this._indexPath; }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        public SearchResultSet Search(string query, int offset = 0, int count = 20, bool includeRawData = false)
        {
            using (TraceSources.ContentSearcherSource.TraceActivity("Search [{1}-{2}]: {0}", query, offset, offset + count))
            {
                SearchResultSet ret = new SearchResultSet();

                //Query q = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "body", this._analyzer).Parse(query);

                Query luceneQuery;

                if (query == "special:all")
                    luceneQuery = new MatchAllDocsQuery();
                else
                {
                    string[] rawTerms = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    BooleanQuery nameQuery = new BooleanQuery();
                    BooleanQuery titleQuery = new BooleanQuery();
                    BooleanQuery summaryQuery = new BooleanQuery();
                    BooleanQuery contentQuery = new BooleanQuery();

                    for (int i = 0; i < rawTerms.Length; i++)
                    {
                        string rawTerm = rawTerms[i];
                        Occur occur;
                        if (rawTerms[i].StartsWith("-"))
                        {
                            rawTerm = rawTerm.Substring(1);
                            occur = Occur.MUST_NOT;
                        }
                        else
                            occur = Occur.MUST;

                        titleQuery.Add(new TermQuery(new Term("name", rawTerm)), occur);

                        titleQuery.Add(new TermQuery(new Term("title", rawTerm)), occur);

                        summaryQuery.Add(new TermQuery(new Term("summary", rawTerm)), occur);

                        contentQuery.Add(new TermQuery(new Term("content", rawTerm)), occur);
                    }

                    BooleanQuery q = new BooleanQuery();

                    titleQuery.Boost = 8f;
                    contentQuery.Boost = 0.7f;

                    q.Add(nameQuery, Occur.SHOULD);
                    q.Add(titleQuery, Occur.SHOULD);
                    q.Add(summaryQuery, Occur.SHOULD);
                    q.Add(contentQuery, Occur.SHOULD);

                    luceneQuery = q;
                }

                TopDocs docs = this._indexSearcher.Search(luceneQuery, offset + count);

                if (docs.ScoreDocs.Length < offset)
                    throw new ArgumentOutOfRangeException("offset", "Offset is smaller than result count!");

                ret.HitCount = docs.TotalHits;

                ret.Results = new SearchResult[Math.Min(docs.ScoreDocs.Length - offset, count)];

                for (int i = 0; i < ret.Results.Length; i++)
                {
                    ScoreDoc scoreDoc = docs.ScoreDocs[offset + i];

                    Document doc = this._indexSearcher.Doc(scoreDoc.Doc);

                    ret.Results[i] = new SearchResult
                                         {
                                             AssetId = AssetIdentifier.Parse(doc.GetField("aid").StringValue),
                                             Title = doc.GetField("title").StringValue,
                                             Url = new Uri(doc.GetField("uri").StringValue, UriKind.RelativeOrAbsolute),
                                             Blurb = doc.GetField("summary").StringValue,
                                             RawDocument = includeRawData ? GetRawData(doc, scoreDoc.Doc).ToArray(): null
                                         };
                }

                return ret;
            }
        }

        private IEnumerable<Tuple<string, string, string[]>> GetRawData(Document doc, int docId)
        {
            foreach (var fieldable in doc.GetFields())
            {
                string name = fieldable.Name;
                string value = fieldable.StringValue;
                List<string> tokens = new List<string>();

                try
                {
                    using (TokenStream tsReader = TokenSources.GetTokenStream(this._indexReader, docId, fieldable.Name))
                    {
                        ITermAttribute attr = tsReader.AddAttribute<ITermAttribute>();
                        while (tsReader.IncrementToken())
                        {
                            tokens.Add(attr.Term);
                        }
                    }
                }
                catch (ArgumentException)
                {
                }
                yield return Tuple.Create(name, value, tokens.ToArray());

            }
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this._indexSearcher.Dispose();
                this._indexReader.Dispose();
                this._directory.Dispose();
            }
        }
    }
}