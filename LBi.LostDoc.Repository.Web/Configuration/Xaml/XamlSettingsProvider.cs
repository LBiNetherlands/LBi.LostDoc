/*
 * Copyright 2013 LBi Netherlands B.V.
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
using System.Threading;
using System.Xaml;
using System.Xml;

namespace LBi.LostDoc.Repository.Web.Configuration.Xaml
{
    public class XamlSettingsProvider : ISettingsProvider
    {
        private readonly ReaderWriterLockSlim _lock;
        private readonly string _filename;
        private readonly Settings _settings;
        private readonly Dictionary<string, Entry> _lookup;

        public XamlSettingsProvider(string filename)
        {
            this._lock = new ReaderWriterLockSlim();
            this._filename = filename;

            if (File.Exists(this._filename))
            {
                XamlXmlReaderSettings readerSettings = new XamlXmlReaderSettings { CloseInput = true };
                using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (XamlXmlReader xamlReader = new XamlXmlReader(file, readerSettings))
                {
                    XamlObjectWriter objectWriter = new XamlObjectWriter(xamlReader.SchemaContext);

                    while (xamlReader.Read())
                        objectWriter.WriteNode(xamlReader);

                    objectWriter.Close();
                    this._settings = (Settings)objectWriter.Result;
                    xamlReader.Close();
                }
            }
            else
                this._settings = new Settings() { Version = 1 };

            this._lookup = this._settings.ToDictionary();
        }

        public void Save(string filename)
        {
            //Assembly[] assemblies = new[] { this._settings.KeyComparer.GetType().Assembly };
            XamlSchemaContext xamlSchemaContext = new XamlSchemaContext();//assemblies);
            using (FileStream file = new FileStream(this._filename, FileMode.Create, FileAccess.Write, FileShare.None))
            using (XmlWriter xmlWriter = XmlWriter.Create(file, new XmlWriterSettings { Indent = true }))
            {
                XamlXmlWriter xamlWriter = new XamlXmlWriter(xmlWriter, xamlSchemaContext);

                XamlObjectReader xamlReader = new XamlObjectReader(this._settings,
                                                                   xamlSchemaContext,
                                                                   new XamlObjectReaderSettings { });

                while (xamlReader.Read())
                {
                    xamlWriter.WriteNode(xamlReader);
                }

                xamlReader.Close();
                xamlWriter.Close();
            }
        }

        public T GetValue<T>(string key)
        {
            this._lock.EnterReadLock();
            try
            {
                Entry entry;
                if (this._lookup.TryGetValue(key, out entry))
                {
                    Entry<T> unpackedEntry = entry as Entry<T>;
                    if (unpackedEntry != null)
                    {
                        return unpackedEntry.Value;
                    }
                    else
                    {
                        throw new Exception("Wrong type: " + key);
                    }
                }
                else
                {
                    throw new KeyNotFoundException(key);
                }
            }
            finally
            {
                this._lock.ExitReadLock();
            }
        }

        public void SetValue<T>(string key, T value)
        {
            this._lock.EnterWriteLock();
            try
            {
                Entry<T> unpackedEntry;
                Entry entry;
                if (this._lookup.TryGetValue(key, out entry))
                    unpackedEntry = entry as Entry<T> ?? new Entry<T> { Key = key };
                else
                {
                    this._lookup.Add(key, unpackedEntry = new Entry<T> { Key = key });
                    this._settings.Entries.Add(unpackedEntry);
                }

                unpackedEntry.Value = value;

                this.Save(this._filename);
            }
            finally
            {
                this._lock.ExitWriteLock();
            }
        }


    }
}
