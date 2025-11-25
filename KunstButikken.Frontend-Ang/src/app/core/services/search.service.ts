import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SearchService {
  readonly visible = signal(false);
  readonly query = signal('');

  toggle(): void {
    this.visible.set(!this.visible());
  }

  show(): void {
    this.visible.set(true);
  }

  hide(): void {
    this.visible.set(false);
  }

  setQuery(query: string): void {
    this.query.set(query);
  }
}
