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
