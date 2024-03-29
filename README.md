# SocialMediaAPIs
A practice ASP.NET core project for Web REStful APIs.
The connection string needs to change to own database login information.

Tables setup and stored procedure required are in the SQL_Query.sql

The Web APIs contains the following functions:
1. User is able to create account
2. User needs to login to use other APIs
3. User can create new post(s)
4. User can like or unlike a post
5. User can create new comment(s) under him/her own posts or other's posts
6. Public users can view users' posts (not need login)
7. User able to get the list of liked posts and their comments

Login authentication is done using JWT (Json Web Token).
