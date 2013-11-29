/*
 * Copyright 2013 DigitasLBi Netherlands B.V.
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

namespace LBi.LostDoc
{
    public interface IAssetVisitor
    {
        void VisitAssembly(Asset asset);
        void VisitNamespace(Asset asset);
        void VisitType(Asset asset);
        void VisitField(Asset asset);
        void VisitEvent(Asset asset);
        void VisitProperty(Asset asset);
        void VisitUnknown(Asset asset);
        void VisitMethod(Asset asset);
    }
}