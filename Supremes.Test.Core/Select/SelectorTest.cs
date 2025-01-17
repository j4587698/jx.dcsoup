﻿using System;
using NUnit.Framework;
using Supremes.Nodes;

#if (NETSTANDARD1_3)
namespace Supremes.Test.Select
#else
namespace Supremes.Test.net45.Select
#endif
{
    [TestFixture]
    public class SelectorTest
    {
        [Test]
        public void TestByTag()
        {
            Elements els = Dcsoup.Parse("<div id=1><div id=2><p>Hello</p></div></div><div id=3>").Select("div");
            Assert.AreEqual(3, els.Count);
            Assert.AreEqual("1", els[0].Id);
            Assert.AreEqual("2", els[1].Id);
            Assert.AreEqual("3", els[2].Id);

            Elements none = Dcsoup.Parse("<div id=1><div id=2><p>Hello</p></div></div><div id=3>").Select("span");
            Assert.AreEqual(0, none.Count);
        }

        [Test]
        public void TestById()
        {
            Elements els = Dcsoup.Parse("<div><p id=foo>Hello</p><p id=foo>Foo two!</p></div>").Select("#foo");
            Assert.AreEqual(2, els.Count);
            Assert.AreEqual("Hello", els[0].Text);
            Assert.AreEqual("Foo two!", els[1].Text);

            Elements none = Dcsoup.Parse("<div id=1></div>").Select("#foo");
            Assert.AreEqual(0, none.Count);
        }

        [Test]
        public void TestByClass()
        {
            Elements els = Dcsoup.Parse("<p id=0 class='one two'><p id=1 class='one'><p id=2 class='two'>").Select("p.one");
            Assert.AreEqual(2, els.Count);
            Assert.AreEqual("0", els[0].Id);
            Assert.AreEqual("1", els[1].Id);

            Elements none = Dcsoup.Parse("<div class='one'></div>").Select(".foo");
            Assert.AreEqual(0, none.Count);

            Elements els2 = Dcsoup.Parse("<div class='One-Two'></div>").Select(".one-two");
            Assert.AreEqual(1, els2.Count);
        }

        [Test]
        public void TestByAttribute()
        {
            string h = "<div Title=Foo /><div Title=Bar /><div Style=Qux /><div title=Bam /><div title=SLAM />" +
                   "<div data-name='with spaces'/>";
            Document doc = Dcsoup.Parse(h);

            Elements withTitle = doc.Select("[title]");
            Assert.AreEqual(4, withTitle.Count);

            Elements foo = doc.Select("[title=foo]");
            Assert.AreEqual(1, foo.Count);

            Elements foo2 = doc.Select("[title=\"foo\"]");
            Assert.AreEqual(1, foo2.Count);

            Elements foo3 = doc.Select("[title=\"Foo\"]");
            Assert.AreEqual(1, foo3.Count);

            Elements dataName = doc.Select("[data-name=\"with spaces\"]");
            Assert.AreEqual(1, dataName.Count);
            Assert.AreEqual("with spaces", dataName.First.Attr("data-name"));

            Elements not = doc.Select("div[title!=bar]");
            Assert.AreEqual(5, not.Count);
            Assert.AreEqual("Foo", not.First.Attr("title"));

            Elements starts = doc.Select("[title^=ba]");
            Assert.AreEqual(2, starts.Count);
            Assert.AreEqual("Bar", starts.First.Attr("title"));
            Assert.AreEqual("Bam", starts.Last.Attr("title"));

            Elements ends = doc.Select("[title$=am]");
            Assert.AreEqual(2, ends.Count);
            Assert.AreEqual("Bam", ends.First.Attr("title"));
            Assert.AreEqual("SLAM", ends.Last.Attr("title"));

            Elements contains = doc.Select("[title*=a]");
            Assert.AreEqual(3, contains.Count);
            Assert.AreEqual("Bar", contains.First.Attr("title"));
            Assert.AreEqual("SLAM", contains.Last.Attr("title"));
        }

        [Test]
        public void TestNamespacedTag()
        {
            Document doc = Dcsoup.Parse("<div><abc:def id=1>Hello</abc:def></div> <abc:def class=bold id=2>There</abc:def>");
            Elements byTag = doc.Select("abc|def");
            Assert.AreEqual(2, byTag.Count);
            Assert.AreEqual("1", byTag.First.Id);
            Assert.AreEqual("2", byTag.Last.Id);

            Elements byAttr = doc.Select(".bold");
            Assert.AreEqual(1, byAttr.Count);
            Assert.AreEqual("2", byAttr.Last.Id);

            Elements byTagAttr = doc.Select("abc|def.bold");
            Assert.AreEqual(1, byTagAttr.Count);
            Assert.AreEqual("2", byTagAttr.Last.Id);

            Elements byContains = doc.Select("abc|def:contains(e)");
            Assert.AreEqual(2, byContains.Count);
            Assert.AreEqual("1", byContains.First.Id);
            Assert.AreEqual("2", byContains.Last.Id);
        }

        [Test]
        public void TestByAttributeStarting()
        {
            Document doc = Dcsoup.Parse("<div id=1 data-name=jsoup>Hello</div><p data-val=5 id=2>There</p><p id=3>No</p>");
            Elements withData = doc.Select("[^data-]");
            Assert.AreEqual(2, withData.Count);
            Assert.AreEqual("1", withData.First.Id);
            Assert.AreEqual("2", withData.Last.Id);

            withData = doc.Select("p[^data-]");
            Assert.AreEqual(1, withData.Count);
            Assert.AreEqual("2", withData.First.Id);
        }

        [Test]
        public void TestByAttributeRegex()
        {
            Document doc = Dcsoup.Parse("<p><img src=foo.png id=1><img src=bar.jpg id=2><img src=qux.JPEG id=3><img src=old.gif><img></p>");
            Elements imgs = doc.Select("img[src~=(?i)\\.(png|jpe?g)]");
            Assert.AreEqual(3, imgs.Count);
            Assert.AreEqual("1", imgs[0].Id);
            Assert.AreEqual("2", imgs[1].Id);
            Assert.AreEqual("3", imgs[2].Id);
        }

        [Test]
        public void TestByAttributeRegexCharacterClass()
        {
            Document doc = Dcsoup.Parse("<p><img src=foo.png id=1><img src=bar.jpg id=2><img src=qux.JPEG id=3><img src=old.gif id=4></p>");
            Elements imgs = doc.Select("img[src~=[o]]");
            Assert.AreEqual(2, imgs.Count);
            Assert.AreEqual("1", imgs[0].Id);
            Assert.AreEqual("4", imgs[1].Id);
        }

        [Test]
        public void TestByAttributeRegexCombined()
        {
            Document doc = Dcsoup.Parse("<div><table class=x><td>Hello</td></table></div>");
            Elements els = doc.Select("div table[class~=x|y]");
            Assert.AreEqual(1, els.Count);
            Assert.AreEqual("Hello", els.Text);
        }

        [Test]
        public void TestCombinedWithContains()
        {
            Document doc = Dcsoup.Parse("<p id=1>One</p><p>Two +</p><p>Three +</p>");
            Elements els = doc.Select("p#1 + :contains(+)");
            Assert.AreEqual(1, els.Count);
            Assert.AreEqual("Two +", els.Text);
            Assert.AreEqual("p", els.First.TagName);
        }

        [Test]
        public void TestAllElements()
        {
            string h = "<div><p>Hello</p><p><b>there</b></p></div>";
            Document doc = Dcsoup.Parse(h);
            Elements allDoc = doc.Select("*");
            Elements allUnderDiv = doc.Select("div *");
            Assert.AreEqual(8, allDoc.Count);
            Assert.AreEqual(3, allUnderDiv.Count);
            Assert.AreEqual("p", allUnderDiv.First.TagName);
        }

        [Test]
        public void TestAllWithClass()
        {
            string h = "<p class=first>One<p class=first>Two<p>Three";
            Document doc = Dcsoup.Parse(h);
            Elements ps = doc.Select("*.first");
            Assert.AreEqual(2, ps.Count);
        }

        [Test]
        public void TestGroupOr()
        {
            string h = "<div title=foo /><div title=bar /><div /><p></p><img /><span title=qux>";
            Document doc = Dcsoup.Parse(h);
            Elements els = doc.Select("p,div,[title]");

            Assert.AreEqual(5, els.Count);
            Assert.AreEqual("div", els[0].TagName);
            Assert.AreEqual("foo", els[0].Attr("title"));
            Assert.AreEqual("div", els[1].TagName);
            Assert.AreEqual("bar", els[1].Attr("title"));
            Assert.AreEqual("div", els[2].TagName);
            Assert.IsTrue(els[2].Attr("title").Length == 0); // missing attributes come back as empty string
            Assert.IsFalse(els[2].HasAttr("title"));
            Assert.AreEqual("p", els[3].TagName);
            Assert.AreEqual("span", els[4].TagName);
        }

        [Test]
        public void TestGroupOrAttribute()
        {
            string h = "<div id=1 /><div id=2 /><div title=foo /><div title=bar />";
            Elements els = Dcsoup.Parse(h).Select("[id],[title=foo]");

            Assert.AreEqual(3, els.Count);
            Assert.AreEqual("1", els[0].Id);
            Assert.AreEqual("2", els[1].Id);
            Assert.AreEqual("foo", els[2].Attr("title"));
        }

        [Test]
        public void Descendant()
        {
            string h = "<div class=head><p class=first>Hello</p><p>There</p></div><p>None</p>";
            Document doc = Dcsoup.Parse(h);
            Elements els = doc.Select(".head p");
            Assert.AreEqual(2, els.Count);
            Assert.AreEqual("Hello", els[0].Text);
            Assert.AreEqual("There", els[1].Text);

            Elements p = doc.Select("p.first");
            Assert.AreEqual(1, p.Count);
            Assert.AreEqual("Hello", p[0].Text);

            Elements empty = doc.Select("p .first"); // self, not descend, should not match
            Assert.AreEqual(0, empty.Count);
        }

        [Test]
        public void And()
        {
            string h = "<div id=1 class='foo bar' title=bar name=qux><p class=foo title=bar>Hello</p></div";
            Document doc = Dcsoup.Parse(h);

            Elements div = doc.Select("div.foo");
            Assert.AreEqual(1, div.Count);
            Assert.AreEqual("div", div.First.TagName);

            Elements p = doc.Select("div .foo"); // space indicates like "div *.foo"
            Assert.AreEqual(1, p.Count);
            Assert.AreEqual("p", p.First.TagName);

            Elements div2 = doc.Select("div#1.foo.bar[title=bar][name=qux]"); // very specific!
            Assert.AreEqual(1, div2.Count);
            Assert.AreEqual("div", div2.First.TagName);

            Elements p2 = doc.Select("div *.foo"); // space indicates like "div *.foo"
            Assert.AreEqual(1, p2.Count);
            Assert.AreEqual("p", p2.First.TagName);
        }

        [Test]
        public void DeeperDescendant()
        {
            string h = "<div class=head><p><span class=first>Hello</div><div class=head><p class=first><span>Another</span><p>Again</div>";
            Elements els = Dcsoup.Parse(h).Select("div p .first");
            Assert.AreEqual(1, els.Count);
            Assert.AreEqual("Hello", els.First.Text);
            Assert.AreEqual("span", els.First.TagName);
        }

        [Test]
        public void ParentChildElement()
        {
            string h = "<div id=1><div id=2><div id = 3></div></div></div><div id=4></div>";
            Document doc = Dcsoup.Parse(h);

            Elements divs = doc.Select("div > div");
            Assert.AreEqual(2, divs.Count);
            Assert.AreEqual("2", divs[0].Id); // 2 is child of 1
            Assert.AreEqual("3", divs[1].Id); // 3 is child of 2

            Elements div2 = doc.Select("div#1 > div");
            Assert.AreEqual(1, div2.Count);
            Assert.AreEqual("2", div2[0].Id);
        }

        [Test]
        public void ParentWithClassChild()
        {
            string h = "<h1 class=foo><a href=1 /></h1><h1 class=foo><a href=2 class=bar /></h1><h1><a href=3 /></h1>";
            Document doc = Dcsoup.Parse(h);

            Elements allAs = doc.Select("h1 > a");
            Assert.AreEqual(3, allAs.Count);
            Assert.AreEqual("a", allAs.First.TagName);

            Elements fooAs = doc.Select("h1.foo > a");
            Assert.AreEqual(2, fooAs.Count);
            Assert.AreEqual("a", fooAs.First.TagName);

            Elements barAs = doc.Select("h1.foo > a.bar");
            Assert.AreEqual(1, barAs.Count);
        }

        [Test]
        public void ParentChildStar()
        {
            string h = "<div id=1><p>Hello<p><b>there</b></p></div><div id=2><span>Hi</span></div>";
            Document doc = Dcsoup.Parse(h);
            Elements divChilds = doc.Select("div > *");
            Assert.AreEqual(3, divChilds.Count);
            Assert.AreEqual("p", divChilds[0].TagName);
            Assert.AreEqual("p", divChilds[1].TagName);
            Assert.AreEqual("span", divChilds[2].TagName);
        }

        [Test]
        public void MultiChildDescent()
        {
            string h = "<div id=foo><h1 class=bar><a href=http://example.com/>One</a></h1></div>";
            Document doc = Dcsoup.Parse(h);
            Elements els = doc.Select("div#foo > h1.bar > a[href*=example]");
            Assert.AreEqual(1, els.Count);
            Assert.AreEqual("a", els.First.TagName);
        }

        [Test]
        public void CaseInsensitive()
        {
            string h = "<dIv tItle=bAr><div>"; // mixed case so a simple toLowerCase() on value doesn't catch
            Document doc = Dcsoup.Parse(h);

            Assert.AreEqual(2, doc.Select("DIV").Count);
            Assert.AreEqual(1, doc.Select("DIV[TITLE]").Count);
            Assert.AreEqual(1, doc.Select("DIV[TITLE=BAR]").Count);
            Assert.AreEqual(0, doc.Select("DIV[TITLE=BARBARELLA").Count);
        }

        [Test]
        public void AdjacentSiblings()
        {
            string h = "<ol><li>One<li>Two<li>Three</ol>";
            Document doc = Dcsoup.Parse(h);
            Elements sibs = doc.Select("li + li");
            Assert.AreEqual(2, sibs.Count);
            Assert.AreEqual("Two", sibs[0].Text);
            Assert.AreEqual("Three", sibs[1].Text);
        }

        [Test]
        public void AdjacentSiblingsWithId()
        {
            string h = "<ol><li id=1>One<li id=2>Two<li id=3>Three</ol>";
            Document doc = Dcsoup.Parse(h);
            Elements sibs = doc.Select("li#1 + li#2");
            Assert.AreEqual(1, sibs.Count);
            Assert.AreEqual("Two", sibs[0].Text);
        }

        [Test]
        public void NotAdjacent()
        {
            string h = "<ol><li id=1>One<li id=2>Two<li id=3>Three</ol>";
            Document doc = Dcsoup.Parse(h);
            Elements sibs = doc.Select("li#1 + li#3");
            Assert.AreEqual(0, sibs.Count);
        }

        [Test]
        public void MixCombinator()
        {
            string h = "<div class=foo><ol><li>One<li>Two<li>Three</ol></div>";
            Document doc = Dcsoup.Parse(h);
            Elements sibs = doc.Select("body > div.foo li + li");

            Assert.AreEqual(2, sibs.Count);
            Assert.AreEqual("Two", sibs[0].Text);
            Assert.AreEqual("Three", sibs[1].Text);
        }

        [Test]
        public void MixCombinatorGroup()
        {
            string h = "<div class=foo><ol><li>One<li>Two<li>Three</ol></div>";
            Document doc = Dcsoup.Parse(h);
            Elements els = doc.Select(".foo > ol, ol > li + li");

            Assert.AreEqual(3, els.Count);
            Assert.AreEqual("ol", els[0].TagName);
            Assert.AreEqual("Two", els[1].Text);
            Assert.AreEqual("Three", els[2].Text);
        }

        [Test]
        public void GeneralSiblings()
        {
            string h = "<ol><li id=1>One<li id=2>Two<li id=3>Three</ol>";
            Document doc = Dcsoup.Parse(h);
            Elements els = doc.Select("#1 ~ #3");
            Assert.AreEqual(1, els.Count);
            Assert.AreEqual("Three", els.First.Text);
        }

        // for http://github.com/jhy/jsoup/issues#issue/10
        [Test]
        public void TestCharactersInIdAndClass()
        {
            // using CSS spec for identifiers (id and class): a-z0-9, -, _. NOT . (which is OK in html spec, but not css)
            string h = "<div><p id='a1-foo_bar'>One</p><p class='b2-qux_bif'>Two</p></div>";
            Document doc = Dcsoup.Parse(h);

            Element el1 = doc.GetElementById("a1-foo_bar");
            Assert.AreEqual("One", el1.Text);
            Element el2 = doc.GetElementsByClass("b2-qux_bif").First;
            Assert.AreEqual("Two", el2.Text);

            Element el3 = doc.Select("#a1-foo_bar").First;
            Assert.AreEqual("One", el3.Text);
            Element el4 = doc.Select(".b2-qux_bif").First;
            Assert.AreEqual("Two", el4.Text);
        }

        // for http://github.com/jhy/jsoup/issues#issue/13
        [Test]
        public void TestSupportsLeadingCombinator()
        {
            string h = "<div><p><span>One</span><span>Two</span></p></div>";
            Document doc = Dcsoup.Parse(h);

            Element p = doc.Select("div > p").First;
            Elements spans = p.Select("> span");
            Assert.AreEqual(2, spans.Count);
            Assert.AreEqual("One", spans.First.Text);

            // make sure doesn't get nested
            h = "<div id=1><div id=2><div id=3></div></div></div>";
            doc = Dcsoup.Parse(h);
            Element div = doc.Select("div").Select(" > div").First;
            Assert.AreEqual("2", div.Id);
        }

        [Test]
        public void TestPseudoLessThan()
        {
            Document doc = Dcsoup.Parse("<div><p>One</p><p>Two</p><p>Three</>p></div><div><p>Four</p>");
            Elements ps = doc.Select("div p:lt(2)");
            Assert.AreEqual(3, ps.Count);
            Assert.AreEqual("One", ps[0].Text);
            Assert.AreEqual("Two", ps[1].Text);
            Assert.AreEqual("Four", ps[2].Text);
        }

        [Test]
        public void TestPseudoGreaterThan()
        {
            Document doc = Dcsoup.Parse("<div><p>One</p><p>Two</p><p>Three</p></div><div><p>Four</p>");
            Elements ps = doc.Select("div p:gt(0)");
            Assert.AreEqual(2, ps.Count);
            Assert.AreEqual("Two", ps[0].Text);
            Assert.AreEqual("Three", ps[1].Text);
        }

        [Test]
        public void TestPseudoEquals()
        {
            Document doc = Dcsoup.Parse("<div><p>One</p><p>Two</p><p>Three</>p></div><div><p>Four</p>");
            Elements ps = doc.Select("div p:eq(0)");
            Assert.AreEqual(2, ps.Count);
            Assert.AreEqual("One", ps[0].Text);
            Assert.AreEqual("Four", ps[1].Text);

            Elements ps2 = doc.Select("div:eq(0) p:eq(0)");
            Assert.AreEqual(1, ps2.Count);
            Assert.AreEqual("One", ps2[0].Text);
            Assert.AreEqual("p", ps2[0].TagName);
        }

        [Test]
        public void TestPseudoBetween()
        {
            Document doc = Dcsoup.Parse("<div><p>One</p><p>Two</p><p>Three</>p></div><div><p>Four</p>");
            Elements ps = doc.Select("div p:gt(0):lt(2)");
            Assert.AreEqual(1, ps.Count);
            Assert.AreEqual("Two", ps[0].Text);
        }

        [Test]
        public void TestPseudoCombined()
        {
            Document doc = Dcsoup.Parse("<div class='foo'><p>One</p><p>Two</p></div><div><p>Three</p><p>Four</p></div>");
            Elements ps = doc.Select("div.foo p:gt(0)");
            Assert.AreEqual(1, ps.Count);
            Assert.AreEqual("Two", ps[0].Text);
        }

        [Test]
        public void TestPseudoHas()
        {
            Document doc = Dcsoup.Parse("<div id=0><p><span>Hello</span></p></div> <div id=1><span class=foo>There</span></div> <div id=2><p>Not</p></div>");

            Elements divs1 = doc.Select("div:has(span)");
            Assert.AreEqual(2, divs1.Count);
            Assert.AreEqual("0", divs1[0].Id);
            Assert.AreEqual("1", divs1[1].Id);

            Elements divs2 = doc.Select("div:has([class]");
            Assert.AreEqual(1, divs2.Count);
            Assert.AreEqual("1", divs2[0].Id);

            Elements divs3 = doc.Select("div:has(span, p)");
            Assert.AreEqual(3, divs3.Count);
            Assert.AreEqual("0", divs3[0].Id);
            Assert.AreEqual("1", divs3[1].Id);
            Assert.AreEqual("2", divs3[2].Id);

            Elements els1 = doc.Body.Select(":has(p)");
            Assert.AreEqual(3, els1.Count); // body, div, dib
            Assert.AreEqual("body", els1.First.TagName);
            Assert.AreEqual("0", els1[1].Id);
            Assert.AreEqual("2", els1[2].Id);
        }

        [Test]
        public void TestNestedHas()
        {
            Document doc = Dcsoup.Parse("<div><p><span>One</span></p></div> <div><p>Two</p></div>");
            Elements divs = doc.Select("div:has(p:has(span))");
            Assert.AreEqual(1, divs.Count);
            Assert.AreEqual("One", divs.First.Text);

            // test matches in has
            divs = doc.Select("div:has(p:matches((?i)two))");
            Assert.AreEqual(1, divs.Count);
            Assert.AreEqual("div", divs.First.TagName);
            Assert.AreEqual("Two", divs.First.Text);

            // test contains in has
            divs = doc.Select("div:has(p:contains(two))");
            Assert.AreEqual(1, divs.Count);
            Assert.AreEqual("div", divs.First.TagName);
            Assert.AreEqual("Two", divs.First.Text);
        }

        [Test]
        public void TestPseudoContains()
        {
            Document doc = Dcsoup.Parse("<div><p>The Rain.</p> <p class=light>The <i>rain</i>.</p> <p>Rain, the.</p></div>");

            Elements ps1 = doc.Select("p:contains(Rain)");
            Assert.AreEqual(3, ps1.Count);

            Elements ps2 = doc.Select("p:contains(the rain)");
            Assert.AreEqual(2, ps2.Count);
            Assert.AreEqual("The Rain.", ps2.First.Html);
            Assert.AreEqual("The <i>rain</i>.", ps2.Last.Html);

            Elements ps3 = doc.Select("p:contains(the Rain):has(i)");
            Assert.AreEqual(1, ps3.Count);
            Assert.AreEqual("light", ps3.First.ClassName);

            Elements ps4 = doc.Select(".light:contains(rain)");
            Assert.AreEqual(1, ps4.Count);
            Assert.AreEqual("light", ps3.First.ClassName);

            Elements ps5 = doc.Select(":contains(rain)");
            Assert.AreEqual(8, ps5.Count); // html, body, div,...
        }

        [Test]
        public void TestPsuedoContainsWithParentheses()
        {
            Document doc = Dcsoup.Parse("<div><p id=1>This (is good)</p><p id=2>This is bad)</p>");

            Elements ps1 = doc.Select("p:contains(this (is good))");
            Assert.AreEqual(1, ps1.Count);
            Assert.AreEqual("1", ps1.First.Id);

            Elements ps2 = doc.Select("p:contains(this is bad\\))");
            Assert.AreEqual(1, ps2.Count);
            Assert.AreEqual("2", ps2.First.Id);
        }

        [Test]
        public void ContainsOwn()
        {
            Document doc = Dcsoup.Parse("<p id=1>Hello <b>there</b> now</p>");
            Elements ps = doc.Select("p:containsOwn(Hello now)");
            Assert.AreEqual(1, ps.Count);
            Assert.AreEqual("1", ps.First.Id);

            Assert.AreEqual(0, doc.Select("p:containsOwn(there)").Count);
        }

        [Test]
        public void TestMatches()
        {
            Document doc = Dcsoup.Parse("<p id=1>The <i>Rain</i></p> <p id=2>There are 99 bottles.</p> <p id=3>Harder (this)</p> <p id=4>Rain</p>");

            Elements p1 = doc.Select("p:matches(The rain)"); // no match, case sensitive
            Assert.AreEqual(0, p1.Count);

            Elements p2 = doc.Select("p:matches((?i)the rain)"); // case insense. should include root, html, body
            Assert.AreEqual(1, p2.Count);
            Assert.AreEqual("1", p2.First.Id);

            Elements p4 = doc.Select("p:matches((?i)^rain$)"); // bounding
            Assert.AreEqual(1, p4.Count);
            Assert.AreEqual("4", p4.First.Id);

            Elements p5 = doc.Select("p:matches(\\d+)");
            Assert.AreEqual(1, p5.Count);
            Assert.AreEqual("2", p5.First.Id);

            Elements p6 = doc.Select("p:matches(\\w+\\s+\\(\\w+\\))"); // test bracket matching
            Assert.AreEqual(1, p6.Count);
            Assert.AreEqual("3", p6.First.Id);

            Elements p7 = doc.Select("p:matches((?i)the):has(i)"); // multi
            Assert.AreEqual(1, p7.Count);
            Assert.AreEqual("1", p7.First.Id);
        }

        [Test]
        public void MatchesOwn()
        {
            Document doc = Dcsoup.Parse("<p id=1>Hello <b>there</b> now</p>");

            Elements p1 = doc.Select("p:matchesOwn((?i)hello now)");
            Assert.AreEqual(1, p1.Count);
            Assert.AreEqual("1", p1.First.Id);

            Assert.AreEqual(0, doc.Select("p:matchesOwn(there)").Count);
        }

        [Test]
        public void TestRelaxedTags()
        {
            Document doc = Dcsoup.Parse("<abc_def id=1>Hello</abc_def> <abc-def id=2>There</abc-def>");

            Elements el1 = doc.Select("abc_def");
            Assert.AreEqual(1, el1.Count);
            Assert.AreEqual("1", el1.First.Id);

            Elements el2 = doc.Select("abc-def");
            Assert.AreEqual(1, el2.Count);
            Assert.AreEqual("2", el2.First.Id);
        }

        [Test]
        public void NotParas()
        {
            Document doc = Dcsoup.Parse("<p id=1>One</p> <p>Two</p> <p><span>Three</span></p>");

            Elements el1 = doc.Select("p:not([id=1])");
            Assert.AreEqual(2, el1.Count);
            Assert.AreEqual("Two", el1.First.Text);
            Assert.AreEqual("Three", el1.Last.Text);

            Elements el2 = doc.Select("p:not(:has(span))");
            Assert.AreEqual(2, el2.Count);
            Assert.AreEqual("One", el2.First.Text);
            Assert.AreEqual("Two", el2.Last.Text);
        }

        [Test]
        public void NotAll()
        {
            Document doc = Dcsoup.Parse("<p>Two</p> <p><span>Three</span></p>");

            Elements el1 = doc.Body.Select(":not(p)"); // should just be the span
            Assert.AreEqual(2, el1.Count);
            Assert.AreEqual("body", el1.First.TagName);
            Assert.AreEqual("span", el1.Last.TagName);
        }

        [Test]
        public void NotClass()
        {
            Document doc = Dcsoup.Parse("<div class=left>One</div><div class=right id=1><p>Two</p></div>");

            Elements el1 = doc.Select("div:not(.left)");
            Assert.AreEqual(1, el1.Count);
            Assert.AreEqual("1", el1.First.Id);
        }

        [Test]
        public void HandlesCommasInSelector()
        {
            Document doc = Dcsoup.Parse("<p name='1,2'>One</p><div>Two</div><ol><li>123</li><li>Text</li></ol>");

            Elements ps = doc.Select("[name=1,2]");
            Assert.AreEqual(1, ps.Count);

            Elements containers = doc.Select("div, li:matches([0-9,]+)");
            Assert.AreEqual(2, containers.Count);
            Assert.AreEqual("div", containers[0].TagName);
            Assert.AreEqual("li", containers[1].TagName);
            Assert.AreEqual("123", containers[1].Text);
        }

        [Test]
        public void SelectSupplementaryCharacter()
        {
            string s = char.ConvertFromUtf32(135361);
            Document doc = Dcsoup.Parse("<div k" + s + "='" + s + "'>^" + s + "$/div>");
            Assert.AreEqual("div", doc.Select("div[k" + s + "]").First.TagName);
            Assert.AreEqual("div", doc.Select("div:containsOwn(" + s + ")").First.TagName);
        }
    }
}
