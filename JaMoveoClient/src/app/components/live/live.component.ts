import { Component, OnInit, OnDestroy, inject, ViewChild, ElementRef } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';
import { SignalRService } from '../../services/signalr.service';
import { Song } from '../../models/song.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-live',
  templateUrl: './live.component.html',
  styleUrls: ['./live.component.scss']
})
export class LiveComponent implements OnInit, OnDestroy {

  @ViewChild('songContainer', { static: false }) songContainer!: ElementRef;

  private destroy$ = new Subject<void>();
  private scrollInterval: any;
  private readonly authService = inject(AuthService);
  private readonly signalRService = inject(SignalRService);
  readonly router = inject(Router);

  currentSong: Song | null = null;
  isAdmin!: boolean;
  isSinger!: boolean;
  autoScroll = false;
  loading = true;
  error = '';
  processedLines: any[] = [];

  private readonly CHAR_WIDTH_PIXELS = 12;
  private readonly HEBREW_CHAR_WIDTH = 14;
  private readonly MIN_CHORD_WIDTH = 30;

  ngOnInit(): void {
    this.isAdmin = this.authService.isAdmin();
    this.isSinger = this.authService.isSinger();
    this.loadCurrentSong();
    this.setupSignalRListeners();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.stopAutoScroll();
  }

  private async loadCurrentSong(): Promise<void> {
    try {
      const cachedSong = this.signalRService.getCurrentSong();

      if (cachedSong) {
        this.currentSong = cachedSong;
        this.formatSongContent();
        this.loading = false;
        return;
      }

    } catch (error) {
      console.error('Error in loadCurrentSong:', error);
      this.error = 'שגיאה בטעינת השיר';
      this.loading = false;
    }
  }

  private setupSignalRListeners(): void {
    this.signalRService.songSelected
      .pipe(takeUntil(this.destroy$))
      .subscribe((song: Song) => {
        this.currentSong = song;
        this.formatSongContent();
        this.loading = false;
        this.error = '';
      });

    this.signalRService.currentSong$
      .pipe(takeUntil(this.destroy$))
      .subscribe((song: Song | null) => {
        if (song) {
          this.currentSong = song;
          this.formatSongContent();
          this.loading = false;
          this.error = '';
        }
      });

    this.signalRService.sessionEnded
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.router.navigate(['/main']);
      });

    this.signalRService.error
      .pipe(takeUntil(this.destroy$))
      .subscribe((error) => {
        this.error = error;
      });
  }

  formatSongContent(): void {
    if (!this.currentSong?.lines) {
      return;
    }

    this.processedLines = this.currentSong.lines.map((line, lineIndex) => ({
      lineIndex,
      words: line.map((wordChord, wordIndex) => ({
        wordIndex,
        lyrics: wordChord.lyrics || '',
        chords: wordChord.chords || '',
        hasChords: !!wordChord.chords,
        width: this.getChordWidth(wordChord.lyrics || ''),
        isHebrew: this.isHebrewText(wordChord.lyrics || '')
      }))
    }));
  }

  getChordWidth(lyrics: string): number {
    if (!lyrics) {
      return this.MIN_CHORD_WIDTH;
    }

    const isHebrew = this.isHebrewText(lyrics);
    const charWidth = isHebrew ? this.HEBREW_CHAR_WIDTH : this.CHAR_WIDTH_PIXELS;
    const baseWidth = lyrics.length * charWidth;

    return Math.max(baseWidth, this.MIN_CHORD_WIDTH);
  }

  private isHebrewText(text: string): boolean {
    return text.split('').some(char => {
      const code = char.charCodeAt(0);
      return code >= 0x0590 && code <= 0x05FF;
    });
  }


  toggleAutoScroll(): void {
    this.autoScroll = !this.autoScroll;
    this.autoScroll ? this.startAutoScroll() : this.stopAutoScroll();
  }

  private startAutoScroll(): void {
    this.scrollInterval = setInterval(() => {
      window.scrollBy(0, 1);
    }, 100);
  }

  private stopAutoScroll(): void {
    if (this.scrollInterval) {
      clearInterval(this.scrollInterval);
      this.scrollInterval = null;
    }
  }

  async quitSession(): Promise<void> {
    if (this.isAdmin) {
      await this.signalRService.quitSession();
    }
  }

  refreshSong(): void {
    this.loading = true;
    this.error = '';
    this.loadCurrentSong();
  }

  getLineDirection(line: any): string {
    if (!line.words || line.words.length === 0) {
      return 'ltr';
    }

    const hebrewWords = line.words.filter((word: any) => word.isHebrew).length;
    const totalWords = line.words.length;

    return hebrewWords > totalWords / 2 ? 'rtl' : 'ltr';
  }

  hasChords(line: any): boolean {
    return line.words.some((word: any) => word.hasChords);
  }

  getWordSpacing(word: any, nextWord: any): number {
    if (!nextWord) return 0;

    const currentWidth = word.width || this.MIN_CHORD_WIDTH;
    const baseSpacing = Math.max(10, currentWidth * 0.1);

    return Math.min(baseSpacing, 30);
  }
}

