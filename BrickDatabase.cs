using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System;

using CsQuery;

namespace LXFPartListCreator
{
    public sealed class BrickDatabase
        : IDisposable
    {
        public const string URL_DESIGN = "https://brickset.com/parts/design-{0}";
        public const string URL_PART = "https://brickset.com/parts/{0}/";
        public const string FILE_INDEX = "index.db";
        public const string FILE_IMAGE = "part-{0}.dat";

        internal static readonly XmlSerializer ser = new XmlSerializer(typeof(BrickDB));

        private readonly FileInfo dbnfo;
        private readonly DirectoryInfo dbdir;
        private readonly Dictionary<int, ColorInfo> colors = new Dictionary<int, ColorInfo>();
        private readonly Dictionary<int, BrickInfo> bricks = new Dictionary<int, BrickInfo>();
        private readonly WebClient wc = new WebClient();
        private bool disposed = false;


        public BrickInfo this[int designID]
        {
            get
            {
                if (!bricks.ContainsKey(designID))
                    AddUpdate(designID);

                return bricks[designID];
            }
            internal set => bricks[designID] = value;
        }

        public void AddUpdate(int designID)
        {
            try
            {
                string html = @"
<!DOCTYPE html>
<!--[if lt IE 7]>      <html class=""no-js lt-ie9 lt-ie8 lt-ie7""> <![endif]-->
<!--[if IE 7]>         <html class=""no-js lt-ie9 lt-ie8""> <![endif]-->
<!--[if IE 8]>         <html class=""no-js lt-ie9""> <![endif]-->
<!--[if gt IE 8]><!-->
<html class=""no-js"" lang=""en"">

<head>
<meta http-equiv=""X-UA-Compatible"" content=""IE=edge""/>
<meta http-equiv=""content-type"" content=""text/html; charset=utf-8""/>
<meta name=""description"" content=""LEGO set database: 3023 ""/>
<meta name=""viewport"" id=""viewport"" content=""width=device-width, minimum-scale=1.0, maximum-scale=1.0""/>
<link rel=""shortcut icon"" href=""/favicon.ico"" type=""image/x-icon""/>
<link rel=""alternate"" type=""application/rss+xml"" title=""Brickset news and activity feed"" href=""//brickset.com/feed/""/>
<link rel=""apple-touch-icon"" href=""/apple-touch-icon.png""/>
<link rel=""canonical"" href=""https://brickset.com/parts/design-3023""/>
<meta name=""temp"" content=""New server""/>
<meta name=""application-name"" content=""Brickset""/>
<meta name=""msapplication-TileColor"" content=""#4667a4""/>
<meta name=""msapplication-TileImage"" content=""//brickset.com/assets/images/icons/windows8.png""/>
<meta name=""p:domain_verify"" content=""891d5f8a6ab61f88cd91190bd2d30152""/>
<meta property=""og:type"" content=""article""/>
<meta property=""og:image"" content=""https://brickset.com/assets/images/logo.png""/>
<meta property=""og:title"" content=""3023 ""/>
<meta property=""og:description"" content/>
<meta property=""og:url"" content=""https://brickset.com/parts/design-3023""/>
<meta property=""og:site_name"" content=""Brickset.com""/>
<meta property=""fb:app_id"" content=""114136156486""/>
<meta name=""twitter:card"" content=""summary""/>
<meta name=""twitter:site"" content=""@brickset""/>
<meta name=""twitter:title"" content=""3023 ""/>
<meta name=""twitter:description"" content/>
<meta name=""twitter:image"" content=""https://brickset.com/assets/images/logo.png""/>
<link href=""//fonts.googleapis.com/css?family=Noto+Sans:400,700,400italic,700italic"" rel=""stylesheet"" type=""text/css"">
<link rel=""stylesheet"" type=""text/css"" href=""/assets/jquery-ui/base/jquery-ui.min.css?v=18""/>
<link rel=""stylesheet"" type=""text/css"" href=""/assets/js/highslide/highslide.css?v=18""/>
<link rel=""stylesheet"" type=""text/css"" href=""/assets/template/css/template.css?v=18""/>
<link rel=""stylesheet"" type=""text/css"" href=""/assets/css/styles.css?v=18""/>
<style>
            div.outerwrap { max-width: 1070px; margin: auto; background-color: white;}
            body { background-color: white; }
        </style>
<script src=""/assets/template/js/vendor/modernizr-2.6.2.min.js""></script>
<script type=""text/javascript"">var tyche = { mode: 'tyche', config: '//config.playwire.com/1016948/v2/websites/63673/banner.json' };</script><script id=""tyche"" src=""//cdn.intergi.com/hera/tyche.js"" type=""text/javascript""></script>
<script type=""text/javascript"">
        var setup = 0;

        var loggedIn = 0;
    </script>
<script>
        (function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){
            (i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),
            m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)
        })(window,document,'script','https://www.google-analytics.com/analytics.js','ga');

        ga('create', 'UA-7345914-1', 'auto');
        ga('set', 'dimension1', 0);
        ga('send', 'pageview');
    </script>
<script type=""text/javascript"">
        var _qevents = _qevents || [];
        (function () {
            var elem = document.createElement('script');
            elem.src = (document.location.protocol == ""https:"" ? ""https://secure"" : ""http://edge"") + "".quantserve.com/quant.js"";
            elem.async = true;
            elem.type = ""text/javascript"";
            var scpt = document.getElementsByTagName('script')[0];
            scpt.parentNode.insertBefore(elem, scpt);
        })();
    </script>

<script type=""text/javascript"">
            window.cookieconsent_options = {""message"":""We use cookies to ensure you get the best experience on our website."",""dismiss"":""Got it!"",""learnMore"":""More info"",""link"":""/about/privacyandcookies"",""theme"":""dark-floating""};
        </script>
<script type=""text/javascript"" src=""//cdnjs.cloudflare.com/ajax/libs/cookieconsent2/1.0.9/cookieconsent.min.js""></script>

<title>3023 | Brickset: LEGO set guide and database</title>
</head>
<body id=""body"" class=""parts"">
<section class=""ad"">
<div data-pw-desk=""leaderboard_atf""></div>
<div data-pw-mobi=""leaderboard_atf""></div>
</section>
<div class=""outerwrap"">
<header role=""banner"" id=""banner"">
<div class=""wrap"">
<div class=""topwrap"">
<a href=""/"" rel=""home"" class=""logo"">
<div class=""hgroup"">
<h1>Brickset</h1>
<h2>Your Lego&reg; set guide</h2>
</div>
</a>
<div class=""country"">
<a title=""Set your preferred country"" href=""#"" style=""background: url(https://images.brickset.com/flags/DE.png)"">
DE
</a>
</div>
<nav role=""navigation"" class=""sitetools"">
<ul>
<li><a href=""/login"" class=""loginLink"">Log in</a></li>
<li><a href=""/signup"" id=""signupLink"">Sign up</a></li>
</ul>
</nav>
<form role=""search"" action=""/search"" id=""searchForm"">
<input type=""text"" class=""keyword"" placeholder=""Search"" id=""searchQuery"" name=""query"" value/>
<select name=""scope"" id=""searchScope"">
<option value=""All"">All</option><option value=""Sets"">Sets</option><option value=""Minifigs"">Minifigs</option><option value=""Parts"">Parts</option><option value=""BrickLists"">BrickLists</option><option value=""News"">News</option><option value=""Members"">Members</option>
</select>
<input type=""submit"" value=""Go""/>
</form>
</div>
<nav role=""navigation"" class=""primarynav"">
<ul>
<li class=""browse haschildren""><a href=""/browse""><span>Browse</span></a>
<div class=""subnav"">
<div class=""wrap"">
<div class=""col"">
<div class=""group"">
<div class=""heading"">Browse our database</div>
<ul>
<li><a href=""/sets"">Sets</a></li>
<li><a href=""/minifigs"">Minifigs</a></li>
<li><a href=""/parts"">Parts</a></li>
<li><a href=""/colours"">Colours</a></li>
<li>&nbsp;</li>
<li><a href=""/instructions"">Instructions</a></li>
<li><a href=""/inventories"">Inventories</a></li>
<li>&nbsp;</li>
<li><a href=""/bricklists"">BrickLists</a></li>
<li><a href=""/queries"">Queries</a></li>
<li><a href=""/reviews"">Reviews</a></li>
<li><a href=""/news"">News</a></li>
</ul>
</div>
<div class=""group"">
<div class=""heading"">Sets by year</div>
<ul>
<li><a href=""/sets/year-2017"">2017</a></li>
<li><a href=""/sets/year-2016"">2016</a></li>
<li><a href=""/sets/year-2015"">2015</a></li>
<li><a href=""/sets/year-2014"">2014</a></li>
</ul>
</div>
</div>
<div class=""col"">
<div class=""group"">
<div class=""heading"">New this year</div>
<ul>
<li><a href=""/sets/theme-BrickHeadz/year-2017"">BrickHeadz</a></li>
<li><a href=""/sets/theme-City/year-2017"">City</a></li>
<li><a href=""/sets/theme-Collectable-Minifigures/year-2017"">Collectable Minifigures</a></li>
<li><a href=""/sets/theme-Creator/year-2017"">Creator</a></li>
<li><a href=""/sets/theme-DC-Super-Hero-Girls/year-2017"">DC Super Hero Girls</a></li>
<li><a href=""/sets/theme-Dimensions/year-2017"">Dimensions</a></li>
<li><a href=""/sets/theme-Disney/year-2017"">Disney</a></li>
<li><a href=""/sets/theme-Elves/year-2017"">Elves</a></li>
<li><a href=""/sets/theme-Friends/year-2017"">Friends</a></li>
<li><a href=""/sets/theme-Marvel-Super-Heroes/year-2017"">Marvel Super Heroes</a></li>

<li><a href=""/sets/theme-Minecraft/year-2017"">Minecraft</a></li>
<li><a href=""/sets/theme-Nexo-Knights/year-2017"">Nexo Knights</a></li>
<li><a href=""/sets/theme-Ninjago/year-2017"">Ninjago</a></li>
<li><a href=""/sets/theme-Star-Wars/year-2017"">Star Wars</a></li>
<li><a href=""/sets/theme-Technic/year-2017"">Technic</a></li>
<li><a href=""/sets/theme-The-LEGO-Batman-Movie/year-2017"">The LEGO Batman Movie</a></li>
<li><a href=""/sets/theme-The-LEGO-Ninjago-Movie/year-2017"">The LEGO Ninjago Movie</a></li>
<li><a href=""/sets/year-2017/tag-polybag"">Polybags</a></li>
</ul>
</div>
</div>
</div>
</div>
</li>
<li class=""buy haschildren""><a href=""/buy""><span>Buy</span></a>
<div class=""subnav"">
<div class=""wrap"">
<div class=""col"">
<div class=""group"">
<div class=""heading"">Shopping for LEGO?</div>
<ul>
<li>
<a href=""/buy"">Top LEGO bargains</a>
</li>
</ul>
</div>
<div class=""group"">
<div class=""heading"">Buy at Amazon</div>
<ul>
<li><a href=""/buy/amazon"">Amazon price comparator</a></li>
<li><a href=""/buy/country-de/vendor-amazon/order-percentdiscount"">Discounts at Amazon.de</a></li>
</ul>
</div>
<div class=""group"">
<div class=""heading"">Buy at eBay</div>
<ul>
<li><a href=""/buy/ebay"">eBay product search</a></li>
</ul>
</div>
<div class=""group"">
<div class=""heading"">Buy at Catawiki</div>
<ul>
<li><a href=""/buy/catawiki"">Auction lots at Catawiki</a></li>
</ul>
</div>
</div>
<div class=""col"">
<div class=""group"">
<div class=""heading"">Buy at shop.LEGO.com</div>
<ul>
<li><a href=""/buy/country-de/vendor-lego/order-dateadded"">New items at shop.LEGO.com</a></li>
<li><a href=""/buy/country-de/vendor-lego/order-percentdiscount"">Discounts at shop.LEGO.com</a></li>
</ul>
</div><div class=""group"">
<div class=""heading"">Buy at BrickLink</div>
<ul>
<li><a href=""/buy/country-de/vendor-bricklink/order-percentdiscount"">Discounts at BrickLink.com</a></li>
<li><a href=""/buy/country-de/vendor-bricklink/order-dateremoved"">Discontinued LEGO products</a></li>
</ul>
</div><div class=""group"">
<div class=""heading"">More...</div>
 <ul>
<li><a href=""/buy/country-de/order-percentdiscount"">View discounts in Germany</a></li>
<li><a href=""/buy/country-de/order-dateadded"">See what's new in Germany</a></li>
</ul>
</div>
</div>
</div>
</div>
</li>
<li class=""mysets""><a href=""/mycollection""><span>My Sets</span></a></li>
<li class=""forum""><a href=""/forum""><span>Forum</span></a></li>
<li class=""more haschildren""><a href=""/more"">More...</a>
<div class=""subnav"">
<div class=""wrap"">
<div class=""col"">
<div class=""group"">
<div class=""heading"">Lists</div>
<ul>
<li><a href=""/reviews/brickset"">Index of staff reviews</a></li>
<li><a href=""/reviews/videos"">Index of video reviews</a></li>
<li><a href=""/reviews/topreviewers"">Top reviewers</a></li>
<li><a href=""/members"">Members</a></li>
</ul>
</div>
<div class=""group"">
<div class=""heading"">LIbrary</div>
<ul>
<li><a href=""/library/catalogues"">Catalogues</a></li>
<li><a href=""/library/ideasbooks"">Ideas Books</a></li>
<li><a href=""/library"">More...</a></li>
</ul>
</div>
<div class=""group"">
<div class=""heading"">Tools</div>
<ul>
<li><a href=""/tools/webservices"">Web services</a></li>
<li><a href=""/tools/rssindex"">RSS feeds</a></li>
<li><a href=""/tools/ieaccelerator"">IE accelerator</a></li>
<li><a href=""/tools/mobileapps"">Mobile apps</a></li>
<li><a href=""/news/subscribe"">Subscribe to news by email</a></li>
</ul>
</div>
</div>
<div class=""col"">
<div class=""group"">
<div class=""heading"">Help</div>
<ul>
<li><a href=""/search"">Help with searching</a></li>
<li><a href=""/faq"">FAQ / How do I...</a></li>
<li><a href=""/news/category-tutorial"">Tutorials</a></li>
</ul>
</div>
<div class=""group"">
<div class=""heading"">About Brickset</div>
<ul>
<li><a href=""/about"">About the site</a></li>
<li><a href=""/about/privacyandcookies"">Privacy and cookies</a></li>
<li><a href=""/about/noveltypolicy"">Novelty policy</a></li>
<li><a href=""/about/affiliatemarketing"">Affiliate marketing disclosure</a></li>
<li><a href=""/about/statistics"">Site statistics</a></li>
<li><a href=""/infographic"">Infographic</a></li>
<li><a href=""/sitemap"">Site map</a></li>
<li><a href=""/contact"">Contact us</a></li>
</ul>
</div>
</div>
</div>
</div>
</li>
<li class=""other""><a href=""/mymenu"">My menu</a></li>
</ul>
</nav>
<nav role=""navigation"" class=""breadcrumbs"">
<ul><li><a href=""/"">Home</a></li><li><a href=""/browse"">Browse</a></li><li><a href=""/parts"">Parts</a></li><li>3023</li></ul>
</nav>
</div>
</header>
<div class=""content"">
<div role=""main"" class=""fullwidth"">
<section class="" main"">
<aside role=""complementary"" class=""nocols parts"">
<div class=""tagsfilter"">
<div class=""tags"">
<a href=""http://brickset.com/parts""><span>3023</span></a>
</div>
<div class=""addtags"">
<form><select><option>Choose a colour</option><option value=""/parts/design-3023/colour-Aqua"">Aqua (1)</option><option value=""/parts/design-3023/colour-Black"">Black (1)</option><option value=""/parts/design-3023/colour-Brick-Yellow"">Brick Yellow (1)</option><option value=""/parts/design-3023/colour-Bright-Blue"">Bright Blue (1)</option><option value=""/parts/design-3023/colour-Bright-Orange"">Bright Orange (1)</option><option value=""/parts/design-3023/colour-Bright-Purple"">Bright Purple (1)</option><option value=""/parts/design-3023/colour-Bright-Red"">Bright Red (1)</option><option value=""/parts/design-3023/colour-Bright-Reddish-Violet"">Bright Reddish Violet (2)</option><option value=""/parts/design-3023/colour-Bright-Yellow"">Bright Yellow (1)</option><option value=""/parts/design-3023/colour-Bright-Yellowish-Green"">Bright Yellowish Green (1)</option><option value=""/parts/design-3023/colour-Dark-Azur"">Dark Azur (1)</option><option value=""/parts/design-3023/colour-Dark-Brown"">Dark Brown (1)</option><option value=""/parts/design-3023/colour-Dark-Green"">Dark Green (1)</option><option value=""/parts/design-3023/colour-Dark-Grey"">Dark Grey (1)</option><option value=""/parts/design-3023/colour-Dark-Orange"">Dark Orange (1)</option><option value=""/parts/design-3023/colour-Dark-Stone-Grey"">Dark Stone Grey (1)</option><option value=""/parts/design-3023/colour-Dove-Blue"">Dove Blue (1)</option><option value=""/parts/design-3023/colour-Earth-Blue"">Earth Blue (1)</option><option value=""/parts/design-3023/colour-Earth-Green"">Earth Green (1)</option><option value=""/parts/design-3023/colour-Earth-Orange"">Earth Orange (1)</option><option value=""/parts/design-3023/colour-Flame-Yellowish-Orange"">Flame Yellowish Orange (1)</option><option value=""/parts/design-3023/colour-Grey"">Grey (1)</option><option value=""/parts/design-3023/colour-Lavender"">Lavender (1)</option><option value=""/parts/design-3023/colour-Light-Purple"">Light Purple (1)</option><option value=""/parts/design-3023/colour-Light-Royal-Blue"">Light Royal Blue (1)</option><option value=""/parts/design-3023/colour-Medium-Azur"">Medium Azur (1)</option><option value=""/parts/design-3023/colour-Medium-Blue"">Medium Blue (1)</option><option value=""/parts/design-3023/colour-Medium-Lavender"">Medium Lavender (1)</option><option value=""/parts/design-3023/colour-Medium-Lilac"">Medium Lilac (1)</option><option value=""/parts/design-3023/colour-Medium-Stone-Grey"">Medium Stone Grey (1)</option><option value=""/parts/design-3023/colour-New-Dark-Red"">New Dark Red (1)</option><option value=""/parts/design-3023/colour-Olive-Green"">Olive Green (1)</option><option value=""/parts/design-3023/colour-Reddish-Brown"">Reddish Brown (1)</option><option value=""/parts/design-3023/colour-Sand-Blue"">Sand Blue (1)</option><option value=""/parts/design-3023/colour-Sand-Green"">Sand Green (1)</option><option value=""/parts/design-3023/colour-Sand-Yellow"">Sand Yellow (1)</option><option value=""/parts/design-3023/colour-Spring-Yellowish-Green"">Spring Yellowish Green (1)</option><option value=""/parts/design-3023/colour-White"">White (1)</option></select></form><form><select><option>Choose a year</option><option value=""/parts/design-3023/year-2016"">2016 (2)</option><option value=""/parts/design-3023/year-2015"">2015 (3)</option><option value=""/parts/design-3023/year-2014"">2014 (2)</option><option value=""/parts/design-3023/year-2013"">2013 (2)</option><option value=""/parts/design-3023/year-2012"">2012 (5)</option><option value=""/parts/design-3023/year-2011"">2011 (1)</option><option value=""/parts/design-3023/year-2008"">2008 (1)</option><option value=""/parts/design-3023/year-2005"">2005 (2)</option><option value=""/parts/design-3023/year-2004"">2004 (1)</option><option value=""/parts/design-3023/year-2003"">2003 (6)</option><option value=""/parts/design-3023/year-2002"">2002 (2)</option><option value=""/parts/design-3023/year-2001"">2001 (1)</option><option value=""/parts/design-3023/year-1998"">1998 (2)</option><option value=""/parts/design-3023/year-1996"">1996 (1)</option><option value=""/parts/design-3023/year-1994"">1994 (1)</option><option value=""/parts/design-3023/year-1993"">1993 (1)</option><option value=""/parts/design-3023/year-1992"">1992 (2)</option><option value=""/parts/design-3023/year-1991"">1991 (1)</option><option value=""/parts/design-3023/year-1990"">1990 (1)</option><option value=""/parts/design-3023/year-1986"">1986 (1)</option><option value=""/parts/design-3023/year-1981"">1981 (1)</option></select></form>
</div>
</div>
<div class=""resultsfilter"">
<div class=""results"">1 to 39 of 39 matches</div>
<ul class=""pagelength"">
<li><a href=""#"">25</a></li><li class=""active""><span>50</span></li><li><a href=""#"">100</a></li><li><a href=""#"">200</a></li>
</ul>
<form class=""sort"">
<label for=""sortby"">Sort by</label>
<select name=""sortsetsby"" id=""sortby"">
<option selected value=""ElementName"">Element name</option><option value=""Category"">Category</option><option value=""Colour"">Colour name</option><option value=""ElementID"">Element number</option><option value=""DesignID"">Design number</option><option value=""rgb"">Colour</option><option value=""setcount"">Set count</option>
</select>
</form>
<ul class=""viewswitch"">
<li class=""active""><a href=""#"">Gallery</a></li><li><a href=""#"">Images</a></li><li><a href=""#"">Designs</a></li>
</ul>
</div>
</aside>
<div class=""iteminfo"">
<div class=""col"">
<img src=""http://cache.lego.com/media/bricks/5/2/302301.jpg""/>
</div>
<div class=""col"">
<h1>PLATE 1X2<br/>etc.</h1>
<dl>
<dt>Design</dt>
<dd><a href=""/parts/design-3023"">3023</a></dd>
<dt>Category</dt>
<dd><a href=""/parts/category-System"">System</a></dd>
<dt>Produced</dt>
<dd>1981 - 2017</dd>
<dt>Element count</dt>
<dd>39</dd>
<dt>Appears in</dt>
<dd><a class=""plain"" href=""/sets/containing-design-3023"">7335 sets</a></dd>
</dl>
</div>
<div class=""col"">
<dl class=""buylinks"">
<dt>Buy this part at</dt>
<dd>
<ul>
<li>
<a class=""bricklink"" href=""http://alpha.bricklink.com/pages/clone/searchproduct.page?q=3023&ss=DE"">BrickLink</a>
</li>
</ul>
</dd>
</dl>
</div>
</div>
<div class=""partlist"">
<ul><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/302301.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/302301.jpg"" title=""Plate 1X2, White"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/302301/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/302301/plate-1x2"">302301</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-White"">White</a> <a href=""/parts/year-1992"">1992</a></div><div class=""floatright"">&copy;1992 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/302301/plate-1x2"">302301</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1992</li><li><a href=""/sets/containing-part-302301"">In 845 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/302302.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/302302.jpg"" title=""Plate 1X2, Grey"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/302302/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/302302/plate-1x2"">302302</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Grey"">Grey</a> <a href=""/parts/year-1990"">1990</a></div><div class=""floatright"">&copy;1990 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/302302/plate-1x2"">302302</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1990</li><li><a href=""/sets/containing-part-302302"">In 202 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/302321.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/302321.jpg"" title=""Plate 1X2, Bright Red"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/302321/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/302321/plate-1x2"">302321</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Bright-Red"">Bright Red</a> <a href=""/parts/year-1986"">1986</a></div><div class=""floatright"">&copy;1986 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/302321/plate-1x2"">302321</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1986</li><li><a href=""/sets/containing-part-302321"">In 691 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/302323.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/302323.jpg"" title=""Plate 1X2, Bright Blue"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/302323/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/302323/plate-1x2"">302323</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Bright-Blue"">Bright Blue</a> <a href=""/parts/year-1992"">1992</a></div><div class=""floatright"">&copy;1992 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/302323/plate-1x2"">302323</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1992</li><li><a href=""/sets/containing-part-302323"">In 432 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/302324.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/302324.jpg"" title=""Plate 1X2, Bright Yellow"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/302324/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/302324/plate-1x2"">302324</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Bright-Yellow"">Bright Yellow</a> <a href=""/parts/year-1981"">1981</a></div><div class=""floatright"">&copy;1981 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/302324/plate-1x2"">302324</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1981</li><li><a href=""/sets/containing-part-302324"">In 493 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/302325.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/302325.jpg"" title=""Plate 1X2, Earth Orange"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/302325/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/302325/plate-1x2"">302325</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Earth-Orange"">Earth Orange</a> <a href=""/parts/year-1996"">1996</a></div><div class=""floatright"">&copy;1996 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/302325/plate-1x2"">302325</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1996</li><li><a href=""/sets/containing-part-302325"">In 35 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/302326.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/302326.jpg"" title=""Plate 1X2, Black"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/302326/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/302326/plate-1x2"">302326</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Black"">Black</a> <a href=""/parts/year-1991"">1991</a></div><div class=""floatright"">&copy;1991 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/302326/plate-1x2"">302326</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1991</li><li><a href=""/sets/containing-part-302326"">In 1061 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/302328.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/302328.jpg"" title=""Plate 1X2, Dark Green"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/302328/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/302328/plate-1x2"">302328</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Dark-Green"">Dark Green</a> <a href=""/parts/year-1994"">1994</a></div><div class=""floatright"">&copy;1994 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/302328/plate-1x2"">302328</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1994</li><li><a href=""/sets/containing-part-302328"">In 265 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4111983.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4111983.jpg"" title=""Plate 1X2, Dark Grey"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4111983/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4111983/plate-1x2"">4111983</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Dark-Grey"">Dark Grey</a> <a href=""/parts/year-1998"">1998</a></div><div class=""floatright"">&copy;1998 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4111983/plate-1x2"">4111983</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1998</li><li><a href=""/sets/containing-part-4111983"">In 71 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4113917.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4113917.jpg"" title=""Plate 1X2, Brick Yellow"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4113917/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4113917/plate-1x2"">4113917</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Brick-Yellow"">Brick Yellow</a> <a href=""/parts/year-1998"">1998</a></div><div class=""floatright"">&copy;1998 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4113917/plate-1x2"">4113917</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1998</li><li><a href=""/sets/containing-part-4113917"">In 364 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4164037.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4164037.jpg"" title=""Plate 1X2, Bright Yellowish Green"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4164037/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4164037/plate-1x2"">4164037</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Bright-Yellowish-Green"">Bright Yellowish Green</a> <a href=""/parts/year-2003"">2003</a></div><div class=""floatright"">&copy;2003 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4164037/plate-1x2"">4164037</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2003</li><li><a href=""/sets/containing-part-4164037"">In 179 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4167468.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4167468.jpg"" title=""Plate 1X2, Sand Blue"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4167468/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4167468/plate-1x2"">4167468</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Sand-Blue"">Sand Blue</a> <a href=""/parts/year-2003"">2003</a></div><div class=""floatright"">&copy;2003 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4167468/plate-1x2"">4167468</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2003</li><li><a href=""/sets/containing-part-4167468"">In 4 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4177932.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4177932.jpg"" title=""Plate 1X2, Bright Orange"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4177932/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4177932/plate-1x2"">4177932</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Bright-Orange"">Bright Orange</a> <a href=""/parts/year-2003"">2003</a></div><div class=""floatright"">&copy;2003 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4177932/plate-1x2"">4177932</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2003</li><li><a href=""/sets/containing-part-4177932"">In 168 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4179825.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4179825.jpg"" title=""Plate 1X2, Medium Blue"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4179825/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4179825/plate-1x2"">4179825</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Medium-Blue"">Medium Blue</a> <a href=""/parts/year-2002"">2002</a></div><div class=""floatright"">&copy;2002 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4179825/plate-1x2"">4179825</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2002</li><li><a href=""/sets/containing-part-4179825"">In 82 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4211063.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4211063.jpg"" title=""Plate 1X2, Dark Stone Grey"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4211063/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4211063/plate-1x2"">4211063</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Dark-Stone-Grey"">Dark Stone Grey</a> <a href=""/parts/year-2002"">2002</a></div><div class=""floatright"">&copy;2002 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4211063/plate-1x2"">4211063</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2002</li><li><a href=""/sets/containing-part-4211063"">In 610 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4211150.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4211150.jpg"" title=""Plate 1X2, Reddish Brown"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4211150/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4211150/plate-1x2"">4211150</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Reddish-Brown"">Reddish Brown</a> <a href=""/parts/year-2003"">2003</a></div><div class=""floatright"">&copy;2003 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4211150/plate-1x2"">4211150</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2003</li><li><a href=""/sets/containing-part-4211150"">In 330 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4211398.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4211398.jpg"" title=""Plate 1X2, Medium Stone Grey"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4211398/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4211398/plate-1x2"">4211398</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Medium-Stone-Grey"">Medium Stone Grey</a> <a href=""/parts/year-1993"">1993</a></div><div class=""floatright"">&copy;1993 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4211398/plate-1x2"">4211398</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>1993</li><li><a href=""/sets/containing-part-4211398"">In 664 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4493568.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4493568.jpg"" title=""Plate 1X2, Dove Blue"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4493568/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4493568/plate-1x2"">4493568</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Dove-Blue"">Dove Blue</a> <a href=""/parts/year-2005"">2005</a></div><div class=""floatright"">&copy;2005 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4493568/plate-1x2"">4493568</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2005</li><li><a href=""/sets/containing-part-4493568"">In 1 set</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4528604.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4528604.jpg"" title=""Plate 1X2, Sand Yellow"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4528604/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4528604/plate-1x2"">4528604</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Sand-Yellow"">Sand Yellow</a> <a href=""/parts/year-2008"">2008</a></div><div class=""floatright"">&copy;2008 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4528604/plate-1x2"">4528604</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2008</li><li><a href=""/sets/containing-part-4528604"">In 163 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4528981.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4528981.jpg"" title=""Plate 1X2, Earth Blue"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4528981/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4528981/plate-1x2"">4528981</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Earth-Blue"">Earth Blue</a> <a href=""/parts/year-2004"">2004</a></div><div class=""floatright"">&copy;2004 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4528981/plate-1x2"">4528981</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2004</li><li><a href=""/sets/containing-part-4528981"">In 79 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4539097.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4539097.jpg"" title=""Plate 1X2, New Dark Red"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4539097/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4539097/plate-1x2"">4539097</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-New-Dark-Red"">New Dark Red</a> <a href=""/parts/year-2001"">2001</a></div><div class=""floatright"">&copy;2001 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4539097/plate-1x2"">4539097</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2001</li><li><a href=""/sets/containing-part-4539097"">In 156 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4570877.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4570877.jpg"" title=""Plate 1X2, Dark Orange"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4570877/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4570877/plate-1x2"">4570877</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Dark-Orange"">Dark Orange</a> <a href=""/parts/year-2003"">2003</a></div><div class=""floatright"">&copy;2003 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4570877/plate-1x2"">4570877</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2003</li><li><a href=""/sets/containing-part-4570877"">In 34 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4619511.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4619511.jpg"" title=""Plate 1X2, Medium Azur"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4619511/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4619511/plate-1x2"">4619511</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Medium-Azur"">Medium Azur</a> <a href=""/parts/year-2014"">2014</a></div><div class=""floatright"">&copy;2014 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4619511/plate-1x2"">4619511</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2014</li><li><a href=""/sets/containing-part-4619511"">In 37 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4619512.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4619512.jpg"" title=""Plate 1X2, Medium Lavender"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4619512/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4619512/plate-1x2"">4619512</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Medium-Lavender"">Medium Lavender</a> <a href=""/parts/year-2012"">2012</a></div><div class=""floatright"">&copy;2012 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4619512/plate-1x2"">4619512</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2012</li><li><a href=""/sets/containing-part-4619512"">In 54 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4623160.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4623160.jpg"" title=""Plate 1X2, Bright Reddish Violet"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4623160/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4623160/plate-1x2"">4623160</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Bright-Reddish-Violet"">Bright Reddish Violet</a> <a href=""/parts/year-2011"">2011</a></div><div class=""floatright"">&copy;2011 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4623160/plate-1x2"">4623160</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2011</li><li><a href=""/sets/containing-part-4623160"">In 1 set</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4649765.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4649765.jpg"" title=""Plate 1X2, Light Royal Blue"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4649765/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4649765/plate-1x2"">4649765</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Light-Royal-Blue"">Light Royal Blue</a> <a href=""/parts/year-2013"">2013</a></div><div class=""floatright"">&copy;2013 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4649765/plate-1x2"">4649765</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2013</li><li><a href=""/sets/containing-part-4649765"">In 6 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4653988.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4653988.jpg"" title=""Plate 1X2, Dark Azur"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4653988/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4653988/plate-1x2"">4653988</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Dark-Azur"">Dark Azur</a> <a href=""/parts/year-2012"">2012</a></div><div class=""floatright"">&copy;2012 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4653988/plate-1x2"">4653988</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2012</li><li><a href=""/sets/containing-part-4653988"">In 22 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4654128.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4654128.jpg"" title=""Plate 1X2, Light Purple"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4654128/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4654128/plate-1x2"">4654128</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Light-Purple"">Light Purple</a> <a href=""/parts/year-2012"">2012</a></div><div class=""floatright"">&copy;2012 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4654128/plate-1x2"">4654128</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2012</li><li><a href=""/sets/containing-part-4654128"">In 40 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4655080.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4655080.jpg"" title=""Plate 1X2, Sand Green"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4655080/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4655080/plate-1x2"">4655080</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Sand-Green"">Sand Green</a> <a href=""/parts/year-2012"">2012</a></div><div class=""floatright"">&copy;2012 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/4655080/plate-1x2"">4655080</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2012</li><li><a href=""/sets/containing-part-4655080"">In 20 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/4655695.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/4655695.jpg"" title=""Plate 1X2, Medium Lilac"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/4655695/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/4655695/plate-1x2"">4655695</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Medium-Lilac"">Medium Lilac</a> <a href=""/parts/year-2003"">2003</a></div><div class=""floatright"">&copy;2003 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div> 
</div>
</li><li> <a class=""tag"" href=""/parts/4655695/plate-1x2"">4655695</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2003</li><li><a href=""/sets/containing-part-4655695"">In 55 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/6013102.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/6013102.jpg"" title=""Plate 1X2, Earth Green"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/6013102/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/6013102/plate-1x2"">6013102</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Earth-Green"">Earth Green</a> <a href=""/parts/year-2005"">2005</a></div><div class=""floatright"">&copy;2005 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/6013102/plate-1x2"">6013102</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2005</li><li><a href=""/sets/containing-part-6013102"">In 38 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/6016483.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/6016483.jpg"" title=""Plate 1X2, Olive Green"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/6016483/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/6016483/plate-1x2"">6016483</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Olive-Green"">Olive Green</a> <a href=""/parts/year-2012"">2012</a></div><div class=""floatright"">&copy;2012 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/6016483/plate-1x2"">6016483</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2012</li><li><a href=""/sets/containing-part-6016483"">In 25 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/6028736.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/6028736.jpg"" title=""Plate 1X2, Flame Yellowish Orange"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/6028736/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/6028736/plate-1x2"">6028736</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Flame-Yellowish-Orange"">Flame Yellowish Orange</a> <a href=""/parts/year-2015"">2015</a></div><div class=""floatright"">&copy;2015 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/6028736/plate-1x2"">6028736</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2015</li><li><a href=""/sets/containing-part-6028736"">In 31 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/6057387.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/6057387.jpg"" title=""Plate 1X2, Bright Purple"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
 <h1><a href=""/parts/6057387/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/6057387/plate-1x2"">6057387</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Bright-Purple"">Bright Purple</a> <a href=""/parts/year-2014"">2014</a></div><div class=""floatright"">&copy;2014 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/6057387/plate-1x2"">6057387</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2014</li><li><a href=""/sets/containing-part-6057387"">In 20 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/6058221.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/6058221.jpg"" title=""Plate 1X2, Dark Brown"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/6058221/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/6058221/plate-1x2"">6058221</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Dark-Brown"">Dark Brown</a> <a href=""/parts/year-2013"">2013</a></div><div class=""floatright"">&copy;2013 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/6058221/plate-1x2"">6058221</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2013</li><li><a href=""/sets/containing-part-6058221"">In 22 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/6099190.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/6099190.jpg"" title=""Plate 1X2, Lavender"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/6099190/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/6099190/plate-1x2"">6099190</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Lavender"">Lavender</a> <a href=""/parts/year-2015"">2015</a></div><div class=""floatright"">&copy;2015 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/6099190/plate-1x2"">6099190</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2015</li><li><a href=""/sets/containing-part-6099190"">In 4 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/6099368.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/6099368.jpg"" title=""Plate 1X2, Spring Yellowish Green"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/6099368/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/6099368/plate-1x2"">6099368</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Spring-Yellowish-Green"">Spring Yellowish Green</a> <a href=""/parts/year-2016"">2016</a></div><div class=""floatright"">&copy;2016 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/6099368/plate-1x2"">6099368</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2016</li><li><a href=""/sets/containing-part-6099368"">In 5 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/6103415.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/6103415.jpg"" title=""Plate 1X2, Bright Reddish Violet"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/6103415/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/6103415/plate-1x2"">6103415</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Bright-Reddish-Violet"">Bright Reddish Violet</a> <a href=""/parts/year-2015"">2015</a></div><div class=""floatright"">&copy;2015 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/6103415/plate-1x2"">6103415</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2015</li><li><a href=""/sets/containing-part-6103415"">In 21 sets</a></li></ul></li><li class=""item""><ul><li>
<a href=""http://cache.lego.com/media/bricks/5/2/6146701.jpg"" class=""highslide plain "" onclick=""return hs.expand(this)""><img src=""http://cache.lego.com/media/bricks/5/1/6146701.jpg"" title=""Plate 1X2, Aqua"" onError=""this.src='/assets/images/spacer.png'""/></a>
<div class=""highslide-caption"">
<h1><a href=""/parts/6146701/plate-1x2"">Plate 1X2</a></h1><div class=""tags floatleft""><a href=""/parts/6146701/plate-1x2"">6146701</a> <a href=""/parts/design-3023"">3023</a> <a href=""/parts/category-System"">System</a> <a href=""/parts/colour-Aqua"">Aqua</a> <a href=""/parts/year-2016"">2016</a></div><div class=""floatright"">&copy;2016 LEGO Group</div>
<div class=""pn"">
<a href=""#"" onclick=""return hs.previous(this)"" title=""Previous (left arrow key)"">&#171; Previous</a>
<a href=""#"" onclick=""return hs.next(this)"" title=""Next (right arrow key)"">Next &#187;</a>
</div>
</div>
</li><li> <a class=""tag"" href=""/parts/6146701/plate-1x2"">6146701</a></li><li> <a class=""tag"" href=""/parts/design-3023"">3023</a></li><li>2016</li><li><a href=""/sets/containing-part-6146701"">In 5 sets</a></li></ul></li></ul>
</div>
<aside role=""complementary"" class=""nocols parts"">
</aside>
</section>
</div>
<div id=""ajaxContainer""></div>
</div>
<section class=""ad"">
<div data-pw-desk=""leaderboard_btf""></div>
<div data-fixed=""true"" data-pw-mobi=""leaderboard_btf""></div>
</section>
<footer role=""contentinfo"">
<div class=""wrap"">
<section class=""stats"">
<h3>Site Statistics</h3>
<ul>
<li>There are <mark>14466 items</mark> in the Brickset database.</li>
<li>Brickset members have written <mark>38795 set reviews</mark>.</li>
<li><mark>8184 members</mark> have logged in in the last 24 hours, <mark>14615</mark> in the last 7 days, <mark>23258</mark> in the last month.</li>
<li><mark>387 people</mark> have joined this week. There are now <mark>169514 members</mark>.</li>
<li>Between us we own <mark>16,815,995 sets</mark> worth at least <mark>US$453,960,741</mark> and containing <mark>4,341,106,343 pieces</mark>.</li>
</ul>
</section>
<section class=""contact"">
<h3>Get in touch</h3>
<p>If you have LEGO news, new images or something else to tell us about, send us a message. If you have a lot to tell us, use <a href=""/contact"">this contact form</a>.</p>
<p id=""contactFormResult"" class=""alert""></p>
<form id=""contactForm"" method=""post"" action=""/contact"">
<div class=""col"">
<input type=""text"" placeholder=""Name"" id=""contactFormName"" value name=""name"" required/>
<input type=""email"" placeholder=""Email"" id=""contactFormEmail"" value name=""email"" required/>
</div>
<div class=""col"">
<textarea placeholder=""Message"" id=""contactFormMessage"" name=""message""></textarea>
</div>
<input type=""submit"" value=""Send message""/>
</form>
</section>
<small class=""links"">
<a href=""/faq"">Frequently Asked Questions</a> |
<a href=""/about"">About Brickset</a> |
<a href=""/about/privacyandcookies"">Privacy and Cookies</a> |
<a href=""/about/affiliatemarketing"">Affiliate Marketing Disclosure</a> |
<a href=""/sitemap"">Site Map</a> |
<a href=""/contact"">Contact Us</a>
</small>
<small>LEGO, the LEGO logo, the Minifigure, and the Brick and Knob configurations are trademarks of the LEGO Group of Companies. &copy;2017 The LEGO Group.</small>
<small>Brickset, the Brickset logo and all content not covered by The LEGO Group's copyright is, unless otherwise stated, &copy;1997-2017 Brickset ltd.</small>
</div>
</footer>
</div>
<div id=""toTop"">^ Top</div>
<script src=""//ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js""></script>
<script src=""//ajax.googleapis.com/ajax/libs/jqueryui/1.9.1/jquery-ui.min.js""></script>
<script>window.jQuery || document.write('<script src=""/assets/template/js/vendor/jquery-1.9.1.min.js""><\/script><script src=""/assets/template/js/vendor/jquery-ui-1.9.1.min.js""><\/script>')</script>
<script src=""/assets/js/highslide/highslide-with-html.packed.js""></script>
<script src=""/assets/template/js/main.js?v=18""></script>
<script src=""/assets/js/brickset.js?v=18""></script>
<script src=""//s7.addthis.com/js/300/addthis_widget.js#pubid=hmillington""></script>
<script type=""text/javascript"">
        _qevents.push({ qacct: ""p-62IS0sm0uHgLU"" });
    </script>
<noscript>
        <div style=""display: none;"">
            <img src=""//pixel.quantserve.com/pixel/p-62IS0sm0uHgLU.gif"" height=""1"" width=""1"" alt=""Quantcast""/>
        </div>
    </noscript>
<script type=""text/javascript"">
        
    </script>
</body>
</html>
"; //  wc.DownloadString(string.Format(URL_DESIGN, designID));
                CQ dom = html;
                BrickInfo brick = new BrickInfo();

                var cs_iteminfo = "section.main div.iteminfo";

                var iteminfo = dom[cs_iteminfo];
                var img = dom[cs_iteminfo + " img[src]"][0]?["src"];
                var col2 = dom[cs_iteminfo + " div.col"].ToList()?[1];

                brick.Variations = FetchVariations(dom["section.main div.partlist > ul li.item"].ToList().ToArray() ?? new IDomObject[0]);

                // brick.Name =

            }
            catch (Exception ex)
            {
                ex.Err();
            }
        }

        private BrickVariation[] FetchVariations(IDomObject[] variations)
        {
            var vlist = new List<BrickVariation>();

            foreach (var variation in variations)
                try
                {
                    var a = variation.Cq().Find("ul li a[href] img[title]").ToList();
                    string parturl = a[0]["src"];
                    int partid = parturl.match(@"\/(?<id>[0-9]+)\.(jpe?g|(d|p)ng|bmp|gif|tiff)$", out Match m) ? int.Parse(m.Groups["id"].ToString()) : -1;

                    if (partid != -1)
                    {
                        DownloadImage(parturl, partid);

                        CQ dom = wc.DownloadString(string.Format(URL_PART, partid));
                        var table = dom["section.featurebox div.text"].Find("dl dd").ToList();
                        var prices = from elem in dom["section.buy span.price a"].ToList()
                                     where elem.InnerTextAllowed
                                     let text = elem.InnerText
                                     where text.match(@"[0-9]+\.[0-9]{2}", out m)
                                     select float.Parse(m.ToString());
                        int colid;

                        vlist.Add(new BrickVariation
                        {
                            PartID = partid,
                            ColorID = colid = int.Parse(table[10].TextContent),
                            ProductionDate = GetYear(table[5].TextContent),
                            PriceAvg = prices.Any() ? prices.Average() : float.NaN,
                        });

                        AddUpdateColor(table);
                    }
                }
                catch (Exception ex)
                {
                    ex.Err();
                }

            return vlist.ToArray();
        }

        private static int[] GetYear(string str) => str.Split('-', ',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(int.Parse).ToArray();

        private void AddUpdateColor(List<IDomObject> ctable)
        {
            string txt(int ndx) => ctable[ndx].TextContent.Trim();
            int colid = int.Parse(txt(10));

            if (!colors.ContainsKey(colid))
                colors[colid] = new ColorInfo
                {
                    FetchDate = DateTime.Now.Ticks,
                    Family = txt(6),
                    Name = txt(7),
                    RGB = txt(8),
                    Type = txt(9),
                    ID = colid,
                };
        }

        private void DownloadImage(string uri, int partID)
        {
            FileInfo path = new FileInfo(dbdir.FullName + '/' + string.Format(FILE_IMAGE, partID));

            if (!path.Exists)
                wc.DownloadFile(uri, path.FullName);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                wc?.Dispose();

                Save();
            }

            disposed = true;
        }

        public void Save()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(BrickDatabase));

            if (dbnfo.Exists)
                dbnfo.Delete();

            using (FileStream fs = dbnfo.Create())
                ser.Serialize(fs, new BrickDB
                {
                    Bricks = bricks.Values.ToArray(),
                    Colors = colors.Values.ToArray(),
                });
        }

        public BrickDatabase(string dir)
        {
            dbdir = new DirectoryInfo(dir);

            if (!dbdir.Exists)
                dbdir.Create();

            dbnfo = new FileInfo($"{dbdir.FullName}/{FILE_INDEX}");

            if (dbnfo.Exists)
                try
                {
                    using (FileStream fs = dbnfo.OpenRead())
                    {
                        BrickDB db = ser.Deserialize(fs) as BrickDB;

                        bricks = db.Bricks.ToDictionary(b => b.DesignID, b => b);
                        colors = db.Colors.ToDictionary(b => b.ID, b => b);
                    }
                }
                catch (Exception ex)
                {
                    ex.Err();
                }
        }
    }

    [Serializable, XmlType(AnonymousType = true), XmlRoot(Namespace = "", IsNullable = false)]
    public sealed partial class BrickDB
    {
        [XmlElement, XmlArray("colors")]
        public ColorInfo[] Colors { set; get; }
        [XmlElement, XmlArray("bricks")]
        public BrickInfo[] Bricks { set; get; }
    }

    [Serializable, XmlType(AnonymousType = true)]
    public sealed partial class ColorInfo
    {
        [XmlAttribute]
        public long FetchDate { set; get; }

        [XmlAttribute]
        public int ID { set; get; }
        [XmlAttribute]
        public string Name { set; get; }
        [XmlAttribute]
        public string Family { set; get; }
        [XmlAttribute]
        public string Type { set; get; }
        [XmlAttribute]
        public string RGB { set; get; }
    }

    [Serializable, XmlType(AnonymousType = true)]
    public sealed partial class BrickInfo
    {
        [XmlAttribute]
        public long FetchDate { set; get; }

        [XmlAttribute]
        public int DesignID { set; get; }
        [XmlAttribute]
        public string Name { set; get; }
        [XmlAttribute]
        public string Category { set; get; }
        [XmlElement, XmlArray("year")]
        public int[] ProductionDate { set; get; }
        [XmlElement, XmlArray("vars")]
        public BrickVariation[] Variations { set; get; }
        [XmlIgnore]
        public int PartID => Variations?.Select(_ => _.PartID)?.FirstOrDefault() ?? -1;
    }

    [Serializable, XmlType(AnonymousType = true)]
    public sealed partial class BrickVariation
    {
        [XmlElement, XmlArray("year")]
        public int[] ProductionDate { set; get; }
        [XmlAttribute]
        public int PartID { set; get; }
        [XmlAttribute]
        public int ColorID { set; get; }
        [XmlAttribute]
        public float PriceAvg { set; get; }

        public override string ToString() => $"Color/ID: {ColorID}/{PartID}  (~{PriceAvg}€)";
    }
}
