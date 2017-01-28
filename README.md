# Blocks

## Cache And Redirect

#### Introduction

This is meant to be used to cache a value from the query string and then
forward the user to another page with the same query string parameter. Upon
return to this same page containing the Cache And Redirect block without
the specified query string parameter it will use the value stored in the
cache and again redirect to the target page. If no value is supplied in the
query string and no value is availabe in the cache then the user is
redirected to another page that would, normally, be used to pick the value
for the query string parameter.

#### Use Cases

You have a number of iPads or laptops for the public to use to sign up for
various bits of information, groups, etc. But you do not know which device
will end up at which table so you cannot hard code the home URL to the
correct destination. You can instead create a Configure page that has
buttons that link to the Cache page with the correct query string parameter
defined. The Cache page will store this and send the user on to the Action
page where they can do whatever you want them to do. When the action is
finished they are sent back to the Cache page which will check the cache
and send them back to the Action page with the correct query string
parameter.

#### Configuration

**Setup Page** is the page that the user will be redirected to if there
is currently no cached value and one was not specified in the query string.

**Content Page** is the page the user will be redirected to with the proper
query string parameter, assuming we have a value in cache or have been
given on in our own query string.

**URL Key** is the key that is expected on our query string (or loaded
from cache) and passed to the **Content Page**.

**Cache Time** is the duration in minutes to keep the **URL Key** value
in cache before it is expired. This is a rolling duration, meaning that
each time the value is read from cache the expiration time is extended.

**Session Unique** is a boolean that indicates if you want this to be
session specific or page (block) specific. If *true* then the cache entry
will be unique to each user's session. If *false* then the cache entry
will be shared by all user's visiting this page.

**Block Key** allows you to link multiple blocks on different pages
together. If a value is entered here then all blocks using this same
**Block Key** and **URL Key** will share the same cached value. Leave blank
for unique cached values on each block.

#### Usage

The normal usage will be 3 pages.

* `Page A` - this is the page where the user will select the initial cache
key.
* `Page B` - this is the page that contains this block.
* `Page C` - this is the (initial) page that will peform whatever
tasks you want the user to do.

`Page A` should link to `Page B` with the appropriate key specified in
the query string. `Page B` will then link to `Page C` after storing the
value in cache. Once `Page C` is done doing whatever it needs to do it
links back to `Page B`, which then checks the cache and sends them back
to `Page C` automatically. If it has been more than **Cache Time** minutes
since it was last visited, causing the cached value to expire, then the
user is sent to `Page A` instead.

## Self Serve Search (Kiosk)

#### Introduction

Self Serve Search is meant to be used on a kiosk where you want the user
to search for themselves (usually by phone number) the same way they would
to check-in one of their kids. Instead of performing a particular action
it simply redirects the user to your selected page with a query string
parameter appending to the URL to identify the user (either by Id or Guid).
This then allows you to chain to another module to perform the specific
action you want to on that person.

#### Use Cases

You have a kiosk in your lobby where the user can give, update their
record, etc. You also want them to be able to sign up for a mailing list
without having to enter their username and password (or create a new account
if they do not already have one). Use this module to let them search for
themselves and pass their PersonId to the next page as a query string
parameter. This allows you to identify the user without them to do a full
login.

#### Sample Images

**Search By Phone**

![Phone Search](Documentation/PhoneSearch.jpg)

**Person Select**

![Person Select](Documentation/PersonSelect.jpg)

#### Configuration

**Cancel Page** is the page to direct the user to if they click the
cancel button.

**Continue Page** is the page to direct the user to if they sleect their
person record.

**Use Person GUID** will pass the Person GUID rather than the ID number if
set to true.

**Query String Key** is the key to use when passing the Person ID/GUID to
the **Continue Page**.

**Search Type** is the type of search method to use, either *Phone* or
*Name*.

**_Phone Search_**

**Minimum Length** is the minimum number of digits the user must enter
to perform a search by phone number.

**Maximum Length** is the maximum number of digits the user may enter
when performing a search by phone number.

**_Name Search_**

**Minimum Length** is the minimum number of letters the user must enter to
perform a search by name.


## File Browser

#### Introduction

Out of the box, Rock includes a really nice File Manager block for uploading
and renaming files in your `Content` directory. But it doesn't work so well
for allowing end users to browse those files and download them. The File
Browser block adds support for doing this by simply having you point it to
the location you want users to be able to browse files in.

Users will see all files in the specified directory as well as any
sub-directories, which will allow the user to navigate into them.

#### Sample Images

The first sample is using the default "list style" view of the block which
displays the results in a format users will probably be used to and
instantly recognize it as a file browser. This is generated by the
`FileBrowserList.lava` file.

![List View](Documentation/FileBrowserList.png)

The second sample switched to the "button style" view of the block. This is
designed to resemble the "pages as buttons" view of many of the Administrative
pages in Rock. Users may not recognize it as a file browser right away but
if you just need to display a few pretty buttons for users to click then this
is the way to go. This is generated by the `FileBrowserButtons.lava` file.

![Button View](Documentation/FileBrowserButtons.png)

#### Configuration

**Content Root** is the root folder to present to the user. They will see
this as simply `/` so they will not be confused by extra parts of the path.
This must be a path that is under your application root, so it must begin
with `~/`.

**Lava Template** is, you guessed it, the Lava that is used to generate your
displayed content. To help you along there are a couple full lava files that
you can simply `{% include %}` in your Lava. In fact the list-view style is
in there by default. You do not need to use any of these pre-made files, you
can create your own lava to completely customize the look if you want. But
there is a lot you can do with some simple CSS overrides too!

#### Customization

If you decide to do some serious customization, there are a few Lava variables
defined for you to make things easier.

**Path** is the user-friendly path they are currently browsing. For example
this might be simply `/Images` if they are viewing the `Images` sub-directory.

**ContentPath** contains the URL friendly path they are currently browsing,
meaning it is the path you can use in a hyperlink to get to the files. For
example this might be `/Content/Images`.

**BrowseJavascript** is the method name to call to browse to a different
directory. It is relative to the current directory, so to navigate to the
parent directory pass `..` to this method. To navigate to a sub-directory
simpily pass the name of the sub-directory. The one exception to this rule is
that if you pass `/` then it will be treated as a "go to root folder"
request. When you call this method from an onclick handle be sure to return
the value returned from the function (which is `false`) as this will cancel
any default action, such as trying to navigate away from the page.

**DownloadJavascript** is the method name to call to initiate a download for
the user. You can link directly to the files, as the Lava samples do, but if
you wish to hide the real path you can use this method. The first argument
is the filename (relative to the current directory) to download. So if you
need to download the file `Data.zip` from the current directory they are
viewing you would write something like
`onclick="return {{ DownloadJavascript }}('Data.zip');"`. You can optionally
include a second argument of `false` to indicate that you don't wish the
file to be force-downloaded but opened in the browser instead (only supported
if the file type is known).

**Directories** contains the list of sub-directories. These are standard .NET
`DirectoryInfo` objects and you can check Microsofts documentation for exactly
what properties are available, or use the `ToJSON` filter to view them that
way.

**Files** contains the list of files in this directory. These are standard
.NET `FileInfo` objects and you can check Microsoft's documentation for
exactly what properties are available, or use the `ToJSON` filter to view
them that way.