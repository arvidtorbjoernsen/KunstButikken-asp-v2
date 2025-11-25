import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { map } from 'rxjs/operators';

import { SearchService } from '../../core/services/search.service';
import { ArtCardComponent } from '../../shared/components/art-card/art-card.component';
import { ArtStore } from '../../state/art.store';

@Component({
  standalone: true,
  selector: 'app-art-for-sale-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    TranslateModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    ArtCardComponent,
    MatGridListModule
  ],
  providers: [ArtStore],
  templateUrl: './art-for-sale.component.html',
  styleUrl: './art-for-sale.component.scss'
})
export class ArtForSaleComponent {
  private readonly store = inject(ArtStore);
  private readonly searchService = inject(SearchService);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly art = toSignal(this.store.art$, { initialValue: [] });
  readonly loadingArt = toSignal(this.store.loadingArt$, { initialValue: false });
  readonly error = toSignal(this.store.error$, { initialValue: undefined as string | undefined });
  readonly query = this.searchService.query;

  readonly filteredArt = computed(() => {
    const q = this.query().trim().toLowerCase();
    const list = this.art();
    if (!q) return list;
    return list.filter((a) => {
      const title = `${a.titleEn ?? ''} ${a.titleNb ?? ''}`.toLowerCase();
      const artist = (a.artist ?? '').toLowerCase();
      const seller = (a.sellerDisplayName ?? '').toLowerCase();
      return title.includes(q) || artist.includes(q) || seller.includes(q);
    });
  });
  readonly hasArt = computed(() => this.filteredArt().length > 0);

  // Responsive columns: 1 (XSmall) -> 2 (Small) -> 3 (Medium) -> 4 (Large) -> 5 (XLarge) -> 6 (2XLarge+)
  readonly cols = toSignal(
    this.breakpointObserver.observe([
      Breakpoints.XSmall,
      Breakpoints.Small,
      Breakpoints.Medium,
      Breakpoints.Large,
      Breakpoints.XLarge
    ]).pipe(
      map(result => {
        if (result.breakpoints[Breakpoints.XSmall]) return 1;
        if (result.breakpoints[Breakpoints.Small]) return 2;
        if (result.breakpoints[Breakpoints.Medium]) return 3;
        if (result.breakpoints[Breakpoints.Large]) return 4;
        if (result.breakpoints[Breakpoints.XLarge]) return 5;
        return 6;
      })
    ),
    { initialValue: 4 }
  );

  constructor() {
    this.store.loadArt();
  }
}
