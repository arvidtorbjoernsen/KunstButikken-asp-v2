import { DecimalPipe } from '@angular/common';
import { afterNextRender, ChangeDetectionStrategy, Component, Input, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { UiArt } from '../../../core/models/art.model';

@Component({
  selector: 'app-art-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule, TranslateModule, DecimalPipe, MatProgressSpinnerModule, RouterModule],
  templateUrl: './art-card.component.html',
  styleUrl: './art-card.component.scss'
})
export class ArtCardComponent {
  @Input({ required: true }) art!: UiArt;
  @Input() priority = false; // Set to true for LCP images (first row)

  readonly fallbackImage = 'assets/images/placeholder.png';
  readonly isHydrated = signal(false);
  readonly imageLoaded = signal(false);

  constructor() {
    // Detect when client-side hydration is complete
    afterNextRender(() => {
      this.isHydrated.set(true);
    });
  }

  handleBrokenImage(event: Event): void {
    const target = event.target as HTMLImageElement | null;
    if (target) {
      target.src = this.fallbackImage;
    }
  }

  onImageLoad(): void {
    this.imageLoaded.set(true);
  }
}
