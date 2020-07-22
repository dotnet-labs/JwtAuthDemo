import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-demo-apis',
  templateUrl: './demo-apis.component.html',
  styleUrls: ['./demo-apis.component.css'],
})
export class DemoApisComponent implements OnInit {
  private readonly apiUrl = `${environment.apiUrl}api/values`;
  busy = false;
  values: string[] = [];
  constructor(private http: HttpClient) {}

  ngOnInit(): void {}
  getValues() {
    this.busy = true;
    this.http
      .get<string[]>(this.apiUrl)
      .pipe(finalize(() => (this.busy = false)))
      .subscribe((x) => {
        this.values = x;
      });
  }
}
