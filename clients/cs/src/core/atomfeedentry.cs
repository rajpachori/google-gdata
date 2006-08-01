/* Copyright (c) 2006 Google Inc.
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
#region Using directives

#define USE_TRACING

using System;
using System.Xml;
using System.Net;
using System.IO;
using System.Collections;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.InteropServices;

#endregion

//////////////////////////////////////////////////////////////////////
// <summary>Contains AtomEntry, an object to represent the atom:entry
// element.</summary>
//////////////////////////////////////////////////////////////////////
namespace Google.GData.Client
{

    //////////////////////////////////////////////////////////////////////
    /// <summary>TypeConverter, so that AtomEntry shows up in the property pages
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    [ComVisible(false)]
    public class AtomEntryConverter : ExpandableObjectConverter
    {
        ///<summary>Standard type converter method</summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType) 
        {
            if (destinationType == typeof(AtomEntry))
                return true;
        
            return base.CanConvertTo(context, destinationType);
        }

        ///<summary>Standard type converter method</summary>
        public override object ConvertTo(ITypeDescriptorContext context,CultureInfo culture, object value, System.Type destinationType) 
        {
            AtomEntry entry = value as AtomEntry; 
            if (destinationType == typeof(System.String) && entry != null)
            {
                return "Entry: " + entry.Title;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
        
    }
    /////////////////////////////////////////////////////////////////////////////


    //////////////////////////////////////////////////////////////////////
    /// <summary>AtomEntry object, representing an item in the RSS/Atom feed
    ///  Version 1.0 removed atom-Head
    ///    element atom:entry {
    ///       atomCommonAttributes,
    ///       (atomAuthor*
    ///         atomCategory*
    ///        atomContent?
    ///        atomContributor*
    ///        atomId
    ///        atomLink*
    ///        atomPublished?
    ///        atomRights?
    ///        atomSource?
    ///        atomSummary?
    ///        atomTitle
    ///        atomUpdated
    ///        extensionElement*)
    ///    }
    ///  </summary>
    //////////////////////////////////////////////////////////////////////
    [TypeConverterAttribute(typeof(AtomEntryConverter)), DescriptionAttribute("Expand to see the entry objects for the feed.")]
    public class AtomEntry : AtomBase
    {
        #region standard entry properties as returned by query
        /// <summary>/feed/entry/title property as string</summary> 
        private AtomTextConstruct title;
        /// <summary>/feed/entry/id property as string</summary> 
        private AtomId id;
        /// <summary>/feed/entry/link collection</summary> 
        private AtomLinkCollection   links;
        /// <summary>/feed/entry/updated property as string</summary> 
        private DateTime lastUpdateDate;
        /// <summary>/feed/entry/published property as string</summary> 
        private DateTime publicationDate;
        /// <summary>/feed/entry/author property as Author object</summary> 
        private AtomPersonCollection authors;
        /// <summary>/feed/entry/atomContributor property as Author object</summary> 
        private AtomPersonCollection contributors;
        /// <summary>The "atom:rights" element is a Text construct that conveys a human-readable copyright statement for an entry or feed.</summary> 
        private AtomTextConstruct rights;
        /// <summary>/feed/entry/category/@term property as a list of AtomCategories</summary> 
        private AtomCategoryCollection categories; 
        /// <summary>The "atom:summary" element is a Text construct that conveys a short summary, abstract or excerpt of an entry.</summary> 
        private AtomTextConstruct summary;

        /// <summary>contains the content as an object</summary> 
        private AtomContent content;

        /// <summary>atom:source element</summary> 
        private AtomSource source;
        /// <summary>GData service to use</summary> 
        private IService service;
        /// <summary>holds the owning feed</summary> 
        private AtomFeed feed; 

        

        #endregion



        #region Persistence overloads
        //////////////////////////////////////////////////////////////////////
        /// <summary>Returns the constant representing this XML element.</summary> 
        //////////////////////////////////////////////////////////////////////
        public override string XmlName 
        {
            get { return AtomParserNameTable.XmlAtomEntryElement; }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>saves the inner state of the element</summary> 
        /// <param name="writer">the xmlWriter to save into </param>
        //////////////////////////////////////////////////////////////////////
        protected override void SaveInnerXml(XmlWriter writer)
        {
            // saving title
            Tracing.TraceMsg("Entering save inner XML on AtomEntry");
            if (this.title != null)
            {
                Tracing.TraceMsg("Saving Title: " + this.Title.Text);
                this.Title.SaveToXml(writer);
            }
            if (this.id != null)
            {
                this.Id.SaveToXml(writer);
            }
            foreach (AtomLink link in this.Links )
            {
                link.SaveToXml(writer);
            }
            foreach (AtomPerson person in this.Authors )
            {
                person.SaveToXml(writer);
            }
            foreach (AtomPerson person in this.Contributors )
            {
                person.SaveToXml(writer);
            }
            foreach (AtomCategory category in this.Categories )
            {
                category.SaveToXml(writer);
            }
            if (this.rights != null)
            {
                this.Rights.SaveToXml(writer);
            }
            if (this.summary != null)
            {
                this.Summary.SaveToXml(writer);
            }
            if (this.content != null)
            {
                this.Content.SaveToXml(writer);
            }
            if (this.source != null)
            {
                this.Source.SaveToXml(writer);
            }

            WriteLocalDateTimeElement(writer, AtomParserNameTable.XmlUpdatedElement, this.Updated);
            WriteLocalDateTimeElement(writer, AtomParserNameTable.XmlPublishedElement, this.Published);
        }
        /////////////////////////////////////////////////////////////////////////////
        #endregion


        //////////////////////////////////////////////////////////////////////
        /// <summary>Read only accessor for feed</summary> 
        //////////////////////////////////////////////////////////////////////
        public AtomFeed Feed
        {
            get {return this.feed;}
        }
        /////////////////////////////////////////////////////////////////////////////

        /// <summary>internal method to set the feed</summary> 
        internal void setFeed(AtomFeed feed)
        {
            if (feed != null)
            {
                this.Dirty = true; 
            }
            this.feed = feed; 
        }



        //////////////////////////////////////////////////////////////////////
        /// <summary>helper method to create a new, decoupled entry based on a feedEntry</summary> 
        /// <param name="entryToImport">the entry from a feed that you want to put somewhere else</param>
        /// <returns> the new entry ready to be inserted</returns>
        //////////////////////////////////////////////////////////////////////
        public static AtomEntry ImportFromFeed(AtomEntry entryToImport)
        {
            Tracing.Assert(entryToImport != null, "entryToImport should not be null");
            if (entryToImport == null)
            {
                throw new ArgumentNullException("entryToImport"); 
            }
            AtomEntry entry=null; 
            entry = (AtomEntry)Activator.CreateInstance(entryToImport.GetType(), new Object[] { });
            entry.CopyEntry(entryToImport);

            entry.Id = null; 

            // if the source is empty, set the source to the old feed

            if (entry.Source == null)
            {
                entry.Source = entryToImport.Feed;
            }
            Tracing.TraceInfo("Imported entry: " + entryToImport.Title.Text + " to: " + entry.Title.Text); 
            return entry;
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method for the GData Service to use</summary> 
        //////////////////////////////////////////////////////////////////////
        public IService Service
        {
            get {return this.service;}
            set {this.Dirty = true;  this.service = value;}
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Uri EditUri</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri EditUri
        {
            get 
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceEdit, AtomLink.ATOM_TYPE);
                // scan the link collection
                return link == null ? null : link.HRef;
            }
            set
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceEdit, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink();
                    this.Links.Add(link);
                }
                link.HRef = value;
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor for the self URI</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomUri SelfUri
        {
            get 
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceSelf, AtomLink.ATOM_TYPE);
                // scan the link collection
                return link == null ? null : link.HRef;
            }
            set
            {
                AtomLink link = this.Links.FindService(BaseNameTable.ServiceSelf, AtomLink.ATOM_TYPE);
                if (link == null)
                {
                    link = new AtomLink();
                    this.Links.Add(link);
                }
                link.HRef = value;
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public DateTime UpdateDate</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime Updated
        {
            get {return this.lastUpdateDate;}
            set {this.Dirty = true;  this.lastUpdateDate = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public DateTime PublicationDate</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public DateTime Published
        {
            get {return this.publicationDate;}
            set {this.Dirty = true;  this.publicationDate = value;}
        }
        /////////////////////////////////////////////////////////////////////////////



        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Contributors AtomPersonCollection</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomPersonCollection Authors
        {
            get 
            {
                if (this.authors == null)
                {
                    this.authors = new AtomPersonCollection();
                }
                return this.authors; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Contributors AtomPersonCollection</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomPersonCollection Contributors
        {
            get 
            {
                if (this.contributors == null)
                {
                    this.contributors = new AtomPersonCollection();
                }
                return this.contributors; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Content</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomContent Content
        {
            get 
            {
                if (this.content == null)
                {
                    this.content = new AtomContent();
                }
                return this.content;
            }
            set {this.Dirty = true;  this.content = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string Summary</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomTextConstruct Summary
        {
            get 
            {
                if (this.summary == null)
                {
                    this.summary = new AtomTextConstruct(AtomTextConstructElementType.Summary); 
                }
                return this.summary;
            }
            set {this.Dirty = true;  this.summary = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public Links AtomLinkCollection</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomLinkCollection Links
        {
            get 
            {
                if (this.links == null)
                {
                    this.links = new AtomLinkCollection();
                }
                return this.links; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public AtomCategoryCollection Categories, holds an array of AtomCategory objects</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomCategoryCollection Categories
        {
            get 
            {
                if (this.categories == null)
                {
                    this.categories = new AtomCategoryCollection();
                }
                return this.categories; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public AtomId Id</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomId Id
        {
            get 
            {
                if (this.id == null)
                {
                    this.id = new AtomId();
                }
                return this.id;
            }
            set {this.Dirty = true;  this.id = value;}
        }
        /////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public AtomTextConstruct Title</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomTextConstruct Title
        {
            get 
            {
                if (this.title == null)
                {
                    this.title = new AtomTextConstruct(AtomTextConstructElementType.Title); 
                }
                return this.title;
            }

            set {this.Dirty = true;  this.title = value;}
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>if the entry was copied, represents the source</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomSource Source
        {
            get {return this.source; }
            set 
            {
                this.Dirty = true;  
                AtomFeed feed = value as AtomFeed; 
                if (feed != null)
                {
                    Tracing.TraceInfo("need to copy a feed to a source"); 
                    this.source = new AtomSource(feed); 
                }
                else
                {
                    this.source = value;
                }
                
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>accessor method public string rights</summary> 
        /// <returns> </returns>
        //////////////////////////////////////////////////////////////////////
        public AtomTextConstruct Rights
        {
            get 
            {
                
                if (this.rights == null)
                {
                    this.rights = new AtomTextConstruct(AtomTextConstructElementType.Rights); 
                }
                return this.rights;
            }
            set {this.Dirty = true;  this.rights = value;}
        }
        /////////////////////////////////////////////////////////////////////////////



        #region EDITING


        //////////////////////////////////////////////////////////////////////
        /// <summary>returns whether or not the entry is read-only </summary> 
        //////////////////////////////////////////////////////////////////////
        public bool ReadOnly
        {
            get {
                return this.EditUri == null ? true : false; 
            }
        }
        /////////////////////////////////////////////////////////////////////////////
        

        //////////////////////////////////////////////////////////////////////
        /// <summary>commits the item to the server</summary> 
        /// <returns>throws an exception if an error occured updating, returns 
        /// the updated entry from the service</returns>
        //////////////////////////////////////////////////////////////////////
        public AtomEntry Update()
        {
            if (this.Service != null)
            {
                AtomEntry updatedEntry = Service.Update(this);
                this.CopyEntry(updatedEntry);
                this.MarkElementDirty(false);
                return updatedEntry; 
            }
            return null;
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>deletes the item from the server</summary> 
        /// <returns>throws an exception if an error occured updating</returns>
        /////////////////////////////////////////////////////////////////////
        public void Delete()
        {
            if (this.Service != null)
            {
                Service.Delete(this);
            }
        }
        /////////////////////////////////////////////////////////////////////////////

        //////////////////////////////////////////////////////////////////////
        /// <summary>takes the updated entry returned and sets the properties to this object</summary> 
        /// <param name="updatedEntry"> </param>
        //////////////////////////////////////////////////////////////////////
        protected void CopyEntry(AtomEntry updatedEntry)
        {
            Tracing.Assert(updatedEntry != null, "updatedEntry should not be null");
            if (updatedEntry == null)
            {
                throw new ArgumentNullException("updatedEntry"); 
            }
            
            this.title = updatedEntry.Title;
            this.authors = updatedEntry.Authors;
            this.id = updatedEntry.Id;
            this.links = updatedEntry.Links;
            this.lastUpdateDate = updatedEntry.Updated;
            this.publicationDate = updatedEntry.Published;
            this.authors = updatedEntry.Authors;
            this.rights = updatedEntry.Rights;
            this.categories = updatedEntry.Categories;
            this.summary = updatedEntry.Summary;
            this.content = updatedEntry.Content;
            this.source = updatedEntry.Source;

            this.ExtensionElements.Clear();

	        foreach (Object extension in updatedEntry.ExtensionElements)
	        {
	            this.ExtensionElements.Add(extension);
	        }
        }
        /////////////////////////////////////////////////////////////////////////////


        #endregion

        #region overloaded for property changes, xml:base
        //////////////////////////////////////////////////////////////////////
        /// <summary>just go down the child collections</summary> 
        /// <param name="uriBase"> as currently calculated</param>
        //////////////////////////////////////////////////////////////////////
        internal override void BaseUriChanged(AtomUri uriBase)
        {
            base.BaseUriChanged(uriBase);
            // now pass it to the properties.
            uriBase = new AtomUri(Utilities.CalculateUri(this.Base, uriBase, null));
            
            if (this.Title != null)
            {
                this.Title.BaseUriChanged(uriBase);
            }
            if (this.Id != null)
            {
                this.Id.BaseUriChanged(uriBase);
            }
            foreach (AtomLink link in this.Links )
            {
                link.BaseUriChanged(uriBase);
            }
            foreach (AtomPerson person in this.Authors )
            {
                person.BaseUriChanged(uriBase);
            }
            foreach (AtomPerson person in this.Contributors )
            {
                person.BaseUriChanged(uriBase);
            }
            foreach (AtomCategory category in this.Categories )
            {
                category.BaseUriChanged(uriBase);
            }
            if (this.Rights != null)
            {
                this.Rights.BaseUriChanged(uriBase);
            }
            if (this.Summary != null)
            {
                this.Summary.BaseUriChanged(uriBase);
            }
            if (this.Content != null)
            {
                this.Content.BaseUriChanged(uriBase);
            }
            if (this.Source != null)
            {
                this.Source.BaseUriChanged(uriBase);
            }
        }
        /////////////////////////////////////////////////////////////////////////////

 
        //////////////////////////////////////////////////////////////////////
        /// <summary>calls the action on this object and all children</summary> 
        /// <param name="action">an IAtomBaseAction interface to call </param>
        /// <returns>true or false, pending outcome</returns>
        //////////////////////////////////////////////////////////////////////
        public override bool WalkTree(IBaseWalkerAction action)
        {
            if (base.WalkTree(action) == true)
            {
                return true;
            }
            foreach (AtomPerson person in this.Authors)
            {
                if (person.WalkTree(action) == true)
                    return true;
            }
            // saving Contributors
            foreach (AtomPerson person in this.Contributors)
            {
                if (person.WalkTree(action) == true)
                    return true;
            }
            // saving Categories
            foreach (AtomCategory category in this.Categories )
            {
                if (category.WalkTree(action) == true)
                    return true;
            }
            if (this.id != null)
            {
                if (this.id.WalkTree(action) == true)
                    return true;
            }
            // save the Links
            foreach (AtomLink link in this.Links)
            {
                if (link.WalkTree(action) == true)
                    return true;
            }
            if (this.rights != null)
            {
                if (this.rights.WalkTree(action) == true)
                    return true;
            }
            if (this.title != null)
            {
                if (this.title.WalkTree(action) == true)
                    return true;
            }
            if (this.summary != null)
            {
                if (this.summary.WalkTree(action) == true)
                    return true;
            }
            if (this.content != null)
            {
                if (this.content.WalkTree(action) == true)
                    return true;
            }
            if (this.source != null)
            {
                if (this.source.WalkTree(action) == true)
                    return true;
            }
            // nothing dirty at all
            return false; 
        }
        /////////////////////////////////////////////////////////////////////////////



        #endregion

    }
    /////////////////////////////////////////////////////////////////////////////

}
/////////////////////////////////////////////////////////////////////////////
 
