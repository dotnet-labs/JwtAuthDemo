import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ManagementRoutingModule } from './management-routing.module';
import { ManagementComponent } from './management.component';


@NgModule({
  declarations: [ManagementComponent],
  imports: [
    CommonModule,
    ManagementRoutingModule
  ]
})
export class ManagementModule { }
