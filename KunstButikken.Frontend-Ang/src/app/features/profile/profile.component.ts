
import { Component, inject, signal, effect } from '@angular/core';
import { ProfileService } from '../../core/services/profile.service';
import { UserProfile } from '../../core/models/user-profile.model';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatDividerModule
  ]
})
export class ProfileComponent {
  private readonly profileService = inject(ProfileService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);

  // Signals for reactive state management
  profile = signal<UserProfile | null>(null);
  loading = signal<boolean>(true);
  saving = signal<boolean>(false);
  error = signal<string | null>(null);

  profileForm: FormGroup;

  constructor() {
    // Initialize form with validation
    this.profileForm = this.fb.group({
      displayName: ['', [Validators.required, Validators.minLength(2)]],
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
      address: [''],
      city: [''],
      postalCode: [''],
      country: [''],
      isSeller: [false],
      profileImageUrl: ['']
    });

    // Load profile data on component initialization
    this.loadProfile();

    // Effect to populate form when profile data is loaded
    effect(() => {
      const profileData = this.profile();
      if (profileData) {
        console.log('[ProfileComponent] Populating form with profile data:', profileData);
        console.log('[ProfileComponent] isSeller value:', profileData.isSeller, 'Type:', typeof profileData.isSeller);

        this.profileForm.patchValue({
          displayName: profileData.displayName || '',
          fullName: profileData.fullName || '',
          email: profileData.email || '',
          phoneNumber: profileData.phoneNumber || '',
          address: profileData.address || '',
          city: profileData.city || '',
          postalCode: profileData.postalCode || '',
          country: profileData.country || '',
          isSeller: profileData.isSeller === true, // Explicit boolean comparison
          profileImageUrl: profileData.profileImageUrl || ''
        });

        console.log('[ProfileComponent] Form value after patch:', this.profileForm.value);
        console.log('[ProfileComponent] isSeller form control value:', this.profileForm.get('isSeller')?.value);
      }
    });
  }

  private loadProfile(): void {
    this.loading.set(true);
    this.error.set(null);

    console.log('[ProfileComponent] Loading profile from UserService...');
    this.profileService.getProfile().subscribe({
      next: (profile) => {
        console.log('[ProfileComponent] Profile loaded successfully:', profile);
        this.profile.set(profile);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('[ProfileComponent] Error loading profile:', err);
        console.error('[ProfileComponent] Error URL:', err.url);
        console.error('[ProfileComponent] Error status:', err.status);
        this.error.set(`Failed to load profile. Please try again. (${err.status}: ${err.message})`);
        this.loading.set(false);
        this.snackBar.open('Failed to load profile', 'Close', { duration: 3000 });
      }
    });
  }

  updateProfile(): void {
    if (this.profileForm.invalid) {
      this.snackBar.open('Please fill in all required fields correctly', 'Close', { duration: 3000 });
      return;
    }

    this.saving.set(true);
    const currentProfile = this.profile();

    const updatedProfile: UserProfile = {
      ...currentProfile,
      ...this.profileForm.value
    };

    this.profileService.updateProfile(updatedProfile).subscribe({
      next: () => {
        this.profile.set(updatedProfile);
        this.saving.set(false);
        this.snackBar.open('Profile updated successfully!', 'Close', { duration: 3000 });
      },
      error: (err) => {
        console.error('Error updating profile:', err);
        this.saving.set(false);
        this.snackBar.open('Failed to update profile. Please try again.', 'Close', { duration: 3000 });
      }
    });
  }

  resetForm(): void {
    const profileData = this.profile();
    if (profileData) {
      this.profileForm.patchValue({
        displayName: profileData.displayName || '',
        fullName: profileData.fullName || '',
        email: profileData.email || '',
        phoneNumber: profileData.phoneNumber || '',
        address: profileData.address || '',
        city: profileData.city || '',
        postalCode: profileData.postalCode || '',
        country: profileData.country || '',
        isSeller: profileData.isSeller === true, // Explicit boolean comparison
        profileImageUrl: profileData.profileImageUrl || ''
      });
      this.snackBar.open('Form reset to saved values', 'Close', { duration: 2000 });
    }
  }
}
