import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { MaterialModule } from './shared/material/material.module';
import { AdminModule } from './admin/admin.module';

import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { LoginComponent } from './login/login.component';
import { LoginAuthGuard } from './guards/login-auth.guard';
import { JwtInterceptor } from './shared/interceptors/jwt.interceptor';
import { ProfileComponent } from './profile/profile.component';
import { AuthGuard } from './guards/auth.guard';
import { EditEmailDialogComponent } from './profile/edit-email-dialog/edit-email-dialog.component';
import { EditPasswordDialogComponent } from './profile/edit-password-dialog/edit-password-dialog.component';
import { RegisterComponent } from './register/register.component';
import { CartComponent } from './cart/cart.component';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    LoginComponent,
    ProfileComponent,
    EditEmailDialogComponent,
    EditPasswordDialogComponent,
    RegisterComponent,
    CartComponent,
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    BrowserAnimationsModule,
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      {
        path: 'login',
        component: LoginComponent,
        canActivate: [LoginAuthGuard],
      },
      {
        path: 'profile',
        component: ProfileComponent,
        canActivate: [AuthGuard],
      },
      {
        path: 'register',
        component: RegisterComponent,
        canActivate: [LoginAuthGuard],
      },
      {
        path: 'cart',
        component: CartComponent,
        canActivate: [AuthGuard],
      },
    ]),
    MaterialModule,
    AdminModule,
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
  ],
  entryComponents: [EditEmailDialogComponent, EditPasswordDialogComponent],
  bootstrap: [AppComponent],
})
export class AppModule {}
