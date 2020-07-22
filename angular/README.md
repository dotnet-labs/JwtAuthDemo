# Angular JWT Demo

This is a regular Angular project, which is created by Angular CLI. This project demonstrates the JWT communications with a backend web API project.

In my demo, this project will be served by NGINX using Docker Compose. In order to let NGINX have access to the compiled Angular web app, please run command `npm run deploy:nginx` to save the output files to a `wwwroot` folder in the `nginx` directory.

Alternatively, you can deploy the Angular web app to a `wwwroot` folder in the ASP.NET Core web app and use Kestrel to serve the Angular app as a SPA.

## API BaseURL

Currently, the API BaseURL is set in the `environment.ts` file with a value of `https://localhost:5001`. You can change it to an empty string if the app is served by Kestrel. Or you can modify the value according to the web API app.
